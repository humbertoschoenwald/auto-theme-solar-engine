// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    private const string TemporaryFileSuffix = ".tmp";
    private const string PersistedConfigurationLogMessage = "Configuration persisted.";
    private const string NormalizedConfigurationLogMessage = "Configuration storage was normalized to reduced-precision protected coordinates.";
    private const string ProtectedCoordinatePayloadFormat = "{0:R}|{1:R}";
    private const char ProtectedCoordinateSeparator = '|';
    private const int ProtectedCoordinateComponentCount = 2;
    private const int LatitudeIndex = 0;
    private const int LongitudeIndex = 1;
    private const string InvalidProtectedCoordinatesPayloadDescription = "Protected coordinates payload is invalid.";
    private const string InvalidProtectedCoordinatesParseDescription = "Protected coordinates could not be parsed.";
    private static readonly CompositeFormat s_protectedCoordinatePayloadCompositeFormat = CompositeFormat.Parse(ProtectedCoordinatePayloadFormat);

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

        string tempPath = string.Concat(appPaths.ConfigPath, TemporaryFileSuffix);

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

            logPublisher.Write(PersistedConfigurationLogMessage);
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
        (double latitude, double longitude, bool hasCoordinates) =
            ResolveStoredCoordinates(persistedConfiguration);
        int locationPrecisionDecimals = CoordinatePrecisionPolicy.NormalizeDecimals(
            persistedConfiguration.LocationPrecisionDecimals);
        (double latitude, double longitude, bool hasCoordinates) reducedCoordinates =
            hasCoordinates
                ? ReduceCoordinates(
                    latitude,
                    longitude,
                    locationPrecisionDecimals)
                : default;

        bool hasLegacyPlaintextCoordinates = HasLegacyPlaintextCoordinates(persistedConfiguration);
        requiresRewrite =
            hasLegacyPlaintextCoordinates
            || persistedConfiguration.LocationPrecisionDecimals != locationPrecisionDecimals
            || (hasCoordinates
                && (!CoordinatePrecisionPolicy.AreEquivalent(
                        latitude,
                        reducedCoordinates.latitude)
                    || !CoordinatePrecisionPolicy.AreEquivalent(
                        longitude,
                        reducedCoordinates.longitude)));

        bool isConfigured = persistedConfiguration.IsConfigured && reducedCoordinates.hasCoordinates;

        return new AppConfig
        {
            Latitude = reducedCoordinates.latitude,
            Longitude = reducedCoordinates.longitude,
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
            logPublisher.Write(NormalizedConfigurationLogMessage);
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

    private static PersistedAppConfig BuildPersistedConfiguration(AppConfig configuration)
    {
        int locationPrecisionDecimals = CoordinatePrecisionPolicy.NormalizeDecimals(
            configuration.LocationPrecisionDecimals);
        (double latitude, double longitude, bool hasCoordinates) =
            configuration.IsConfigured
                ? ReduceCoordinates(
                    configuration.Latitude,
                    configuration.Longitude,
                    locationPrecisionDecimals)
                : default;

        return new PersistedAppConfig
        {
            ProtectedCoordinates = hasCoordinates
                ? ProtectCoordinates(latitude, longitude)
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

    private (double latitude, double longitude, bool hasCoordinates) ResolveStoredCoordinates(
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
            s_protectedCoordinatePayloadCompositeFormat,
            latitude,
            longitude);

        byte[] plainBytes = Encoding.UTF8.GetBytes(payload);
        byte[] protectedBytes = WindowsDataProtection.Protect(plainBytes);
        return Convert.ToBase64String(protectedBytes);
    }

    private static (double latitude, double longitude, bool hasCoordinates) UnprotectCoordinates(string protectedCoordinates)
    {
        byte[] protectedBytes = Convert.FromBase64String(protectedCoordinates);
        byte[] plainBytes = WindowsDataProtection.Unprotect(protectedBytes);

        string payload = Encoding.UTF8.GetString(plainBytes);
        string[] parts = payload.Split(ProtectedCoordinateSeparator, StringSplitOptions.TrimEntries);
        if (parts.Length != ProtectedCoordinateComponentCount)
        {
            throw new FormatException(InvalidProtectedCoordinatesPayloadDescription);
        }

        if (!double.TryParse(parts[LatitudeIndex], CultureInfo.InvariantCulture, out double latitude)
            || !double.TryParse(parts[LongitudeIndex], CultureInfo.InvariantCulture, out double longitude))
        {
            throw new FormatException(InvalidProtectedCoordinatesParseDescription);
        }

        Result<GeoCoordinates> coordinates = GeoCoordinates.Create(latitude, longitude);
        return coordinates.IsFailure
            ? throw new FormatException(coordinates.Error.Description)
            : (coordinates.Value.Latitude, coordinates.Value.Longitude, true);
    }

    private static bool HasLegacyPlaintextCoordinates(PersistedAppConfig persistedConfiguration)
    {
        return string.IsNullOrWhiteSpace(persistedConfiguration.ProtectedCoordinates)
            && persistedConfiguration.Latitude is not null
            && persistedConfiguration.Longitude is not null;
    }

    private static (double latitude, double longitude, bool hasCoordinates) ReduceCoordinates(
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
