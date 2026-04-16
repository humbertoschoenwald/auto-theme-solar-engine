using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Infrastructure.Security;
using SolarEngine.Infrastructure.Serialization;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SystemHost.Infrastructure;

internal sealed class ConfigurationRepository(AppPaths appPaths, StructuredLogPublisher logPublisher)
{
    public AppConfig Load()
    {
        try
        {
            _ = Directory.CreateDirectory(appPaths.DirectoryPath);

            if (!File.Exists(appPaths.ConfigPath))
            {
                return new AppConfig();
            }

            PersistedAppConfig? persistedConfiguration;
            using (FileStream stream = new(appPaths.ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                persistedConfiguration = JsonSerializer.Deserialize(
                    stream,
                    AppConfigJsonContext.Default.PersistedAppConfig);
            }

            if (persistedConfiguration is null)
            {
                return new AppConfig();
            }

            AppConfig runtimeConfiguration = BuildRuntimeConfiguration(
                persistedConfiguration,
                out bool requiresRewrite);
            TryRewriteStoredConfigurationIfNeeded(
                runtimeConfiguration,
                requiresRewrite);
            return runtimeConfiguration;
        }
        catch (JsonException exception)
        {
            logPublisher.Write($"Configuration load rejected invalid JSON: {exception.Message}");
            return new AppConfig();
        }
        catch (IOException exception)
        {
            logPublisher.Write($"Configuration load failed due to I/O error: {exception.Message}");
            return new AppConfig();
        }
        catch (UnauthorizedAccessException exception)
        {
            logPublisher.Write($"Configuration load failed due to access restrictions: {exception.Message}");
            return new AppConfig();
        }
    }

    public void Save(AppConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _ = Directory.CreateDirectory(appPaths.DirectoryPath);

        string tempPath = string.Concat(appPaths.ConfigPath, ".tmp");

        try
        {
            using (FileStream stream = new(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PersistedAppConfig persistedConfiguration = BuildPersistedConfiguration(configuration);

                JsonSerializer.Serialize(
                    stream,
                    persistedConfiguration,
                    AppConfigJsonContext.Default.PersistedAppConfig);
                stream.Flush(flushToDisk: true);
            }

            if (File.Exists(appPaths.ConfigPath))
            {
                File.Replace(tempPath, appPaths.ConfigPath, destinationBackupFileName: null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(tempPath, appPaths.ConfigPath);
            }

            logPublisher.Write("Configuration persisted.");
        }
        catch (IOException)
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            TryDeleteTempFile(tempPath);
            throw;
        }
    }

    private AppConfig BuildRuntimeConfiguration(
        PersistedAppConfig persistedConfiguration,
        out bool requiresRewrite)
    {
        (double Latitude, double Longitude, bool HasCoordinates) =
            ResolveStoredCoordinates(persistedConfiguration);
        int locationPrecisionDecimals = CoordinatePrecisionPolicy.NormalizeDecimals(
            persistedConfiguration.LocationPrecisionDecimals);
        (double Latitude, double Longitude, bool HasCoordinates) reducedCoordinates =
            HasCoordinates
                ? ReduceCoordinates(
                    Latitude,
                    Longitude,
                    locationPrecisionDecimals)
                : default;

        bool hasLegacyPlaintextCoordinates = HasLegacyPlaintextCoordinates(persistedConfiguration);
        requiresRewrite =
            hasLegacyPlaintextCoordinates
            || persistedConfiguration.LocationPrecisionDecimals != locationPrecisionDecimals
            || (HasCoordinates
                && (!CoordinatePrecisionPolicy.AreEquivalent(
                        Latitude,
                        reducedCoordinates.Latitude)
                    || !CoordinatePrecisionPolicy.AreEquivalent(
                        Longitude,
                        reducedCoordinates.Longitude)));

        bool isConfigured = persistedConfiguration.IsConfigured && reducedCoordinates.HasCoordinates;

        return new AppConfig
        {
            Latitude = reducedCoordinates.Latitude,
            Longitude = reducedCoordinates.Longitude,
            UseWindowsLocation = persistedConfiguration.UseWindowsLocation,
            LocationPrecisionDecimals = locationPrecisionDecimals,
            StartWithWindows = persistedConfiguration.StartWithWindows,
            StartMinimized = persistedConfiguration.StartMinimized,
            UseHighPriority = persistedConfiguration.UseHighPriority,
            AddExtraMinuteAtSunset = persistedConfiguration.AddExtraMinuteAtSunset,
            AutomaticUpdatesEnabled = persistedConfiguration.AutomaticUpdatesEnabled,
            LanguageCode = AppLanguageCodes.Normalize(persistedConfiguration.LanguageCode),
            CheckIntervalSeconds = persistedConfiguration.CheckIntervalSeconds,
            IsConfigured = isConfigured
        };
    }

    private void TryRewriteStoredConfigurationIfNeeded(
        AppConfig runtimeConfiguration,
        bool requiresRewrite)
    {
        if (!requiresRewrite)
        {
            return;
        }

        try
        {
            Save(runtimeConfiguration);
            logPublisher.Write("Configuration storage was normalized to reduced-precision protected coordinates.");
        }
        catch (IOException exception)
        {
            logPublisher.Write($"Configuration storage normalization failed due to I/O error: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            logPublisher.Write($"Configuration storage normalization failed due to access restrictions: {exception.Message}");
        }
    }

    private PersistedAppConfig BuildPersistedConfiguration(AppConfig configuration)
    {
        int locationPrecisionDecimals = CoordinatePrecisionPolicy.NormalizeDecimals(
            configuration.LocationPrecisionDecimals);
        (double Latitude, double Longitude, bool HasCoordinates) =
            configuration.IsConfigured
                ? ReduceCoordinates(
                    configuration.Latitude,
                    configuration.Longitude,
                    locationPrecisionDecimals)
                : default;

        return new PersistedAppConfig
        {
            ProtectedCoordinates = HasCoordinates
                ? ProtectCoordinates(Latitude, Longitude)
                : null,
            Latitude = null,
            Longitude = null,
            UseWindowsLocation = configuration.UseWindowsLocation,
            LocationPrecisionDecimals = locationPrecisionDecimals,
            StartWithWindows = configuration.StartWithWindows,
            StartMinimized = configuration.StartMinimized,
            UseHighPriority = configuration.UseHighPriority,
            AddExtraMinuteAtSunset = configuration.AddExtraMinuteAtSunset,
            AutomaticUpdatesEnabled = configuration.AutomaticUpdatesEnabled,
            LanguageCode = AppLanguageCodes.Normalize(configuration.LanguageCode),
            CheckIntervalSeconds = configuration.CheckIntervalSeconds,
            IsConfigured = configuration.IsConfigured
        };
    }

    private (double Latitude, double Longitude, bool HasCoordinates) ResolveStoredCoordinates(
        PersistedAppConfig persistedConfiguration)
    {
        if (!string.IsNullOrWhiteSpace(persistedConfiguration.ProtectedCoordinates))
        {
            try
            {
                return UnprotectCoordinates(persistedConfiguration.ProtectedCoordinates);
            }
            catch (Exception exception) when (
                exception is FormatException
                or ArgumentException)
            {
                logPublisher.Write($"Protected coordinates could not be decrypted: {exception.Message}");
                return default;
            }
            catch (Win32Exception exception)
            {
                logPublisher.Write($"Protected coordinates could not be decrypted: {exception.Message}");
                return default;
            }
        }

        if (persistedConfiguration.Latitude is double latitude
            && persistedConfiguration.Longitude is double longitude)
        {
            Result<GeoCoordinates> coordinates = GeoCoordinates.Create(latitude, longitude);
            if (coordinates.IsSuccess)
            {
                return (coordinates.Value.Latitude, coordinates.Value.Longitude, true);
            }

            logPublisher.Write($"Legacy coordinates were rejected: {coordinates.Error.Description}");
        }

        return default;
    }

    private static string ProtectCoordinates(double latitude, double longitude)
    {
        string payload = string.Format(
            CultureInfo.InvariantCulture,
            "{0:R}|{1:R}",
            latitude,
            longitude);

        byte[] plainBytes = Encoding.UTF8.GetBytes(payload);
        byte[] protectedBytes = WindowsDataProtection.Protect(plainBytes);
        return Convert.ToBase64String(protectedBytes);
    }

    private static (double Latitude, double Longitude, bool HasCoordinates) UnprotectCoordinates(string protectedCoordinates)
    {
        byte[] protectedBytes = Convert.FromBase64String(protectedCoordinates);
        byte[] plainBytes = WindowsDataProtection.Unprotect(protectedBytes);

        string payload = Encoding.UTF8.GetString(plainBytes);
        string[] parts = payload.Split('|', StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new FormatException("Protected coordinates payload is invalid.");
        }

        if (!double.TryParse(parts[0], CultureInfo.InvariantCulture, out double latitude)
            || !double.TryParse(parts[1], CultureInfo.InvariantCulture, out double longitude))
        {
            throw new FormatException("Protected coordinates could not be parsed.");
        }

        Result<GeoCoordinates> coordinates = GeoCoordinates.Create(latitude, longitude);
        return coordinates.IsFailure
            ? throw new FormatException(coordinates.Error.Description)
            : ((double Latitude, double Longitude, bool HasCoordinates))(coordinates.Value.Latitude, coordinates.Value.Longitude, true);
    }

    private static bool HasLegacyPlaintextCoordinates(PersistedAppConfig persistedConfiguration)
    {
        return string.IsNullOrWhiteSpace(persistedConfiguration.ProtectedCoordinates)
            && persistedConfiguration.Latitude is not null
            && persistedConfiguration.Longitude is not null;
    }

    private static (double Latitude, double Longitude, bool HasCoordinates) ReduceCoordinates(
        double latitude,
        double longitude,
        int locationPrecisionDecimals)
    {
        return (
            CoordinatePrecisionPolicy.Reduce(latitude, locationPrecisionDecimals),
            CoordinatePrecisionPolicy.Reduce(longitude, locationPrecisionDecimals),
            true);
    }

    private void TryDeleteTempFile(string tempPath)
    {
        if (!File.Exists(tempPath))
        {
            return;
        }

        try
        {
            File.Delete(tempPath);
        }
        catch (IOException exception)
        {
            logPublisher.Write($"Temporary configuration cleanup failed due to I/O error: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            logPublisher.Write($"Temporary configuration cleanup failed due to access restrictions: {exception.Message}");
        }
    }
}
