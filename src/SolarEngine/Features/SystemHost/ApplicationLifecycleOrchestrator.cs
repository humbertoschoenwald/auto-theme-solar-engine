using System.Diagnostics;
using SolarEngine.Features.Locations;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.SystemHost.Infrastructure;
using SolarEngine.Features.Themes;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SystemHost;

internal sealed class ApplicationLifecycleOrchestrator(
    ConfigurationRepository configurationRepository,
    ThemeTransitionOrchestrator themeTransitionOrchestrator,
    WindowsStartupRegistrar windowsStartupRegistrar,
    GetSystemLocationQueryHandler getSystemLocationQueryHandler,
    StructuredLogPublisher logPublisher) : IDisposable
{
    public AppConfig Config { get; private set; } = new();

    public void Initialize()
    {
        Config = configurationRepository.Load();
        windowsStartupRegistrar.SetEnabled(Config.StartWithWindows, GetExecutablePath());
        themeTransitionOrchestrator.UpdateConfiguration(Config);
        ApplyProcessPriority();
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await themeTransitionOrchestrator.StartAsync(cancellationToken).ConfigureAwait(false);
        await ApplyCurrentThemeAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask SaveAsync(AppConfig configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        cancellationToken.ThrowIfCancellationRequested();

        Config = SanitizeConfiguration(configuration with { IsConfigured = true });
        PersistConfigurationState();

        if (Config.UseWindowsLocation)
        {
            Result<GeoCoordinates> refreshResult = await RefreshCoordinatesFromWindowsAsync(cancellationToken).ConfigureAwait(false);
            if (refreshResult.IsFailure)
            {
                logPublisher.Write($"Windows location refresh rejected after save: {refreshResult.Error.Description}");
                await RefreshScheduleAndThemeAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            await RefreshScheduleAndThemeAsync(cancellationToken).ConfigureAwait(false);
        }

        ApplyProcessPriority();
    }

    public ValueTask<Result<GeoCoordinates>> DetectCoordinatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return getSystemLocationQueryHandler.HandleAsync(new GetSystemLocationQuery(), cancellationToken);
    }

    public async ValueTask<Result<GeoCoordinates>> RefreshCoordinatesFromWindowsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!Config.UseWindowsLocation)
        {
            return Result<GeoCoordinates>.Failure(new Error(
                "locations.provider.disabled",
                "Preserve explicit operator intent when Windows location is disabled in configuration."));
        }

        Result<GeoCoordinates> coordinatesResult = await DetectCoordinatesAsync(cancellationToken).ConfigureAwait(false);
        if (coordinatesResult.IsFailure)
        {
            return coordinatesResult;
        }

        GeoCoordinates coordinates = CoordinatePrecisionPolicy.Reduce(
            coordinatesResult.Value,
            Config.LocationPrecisionDecimals);
        Config = Config with
        {
            Latitude = coordinates.Latitude,
            Longitude = coordinates.Longitude,
            IsConfigured = true
        };

        PersistConfigurationState();

        await RefreshScheduleAndThemeAsync(cancellationToken).ConfigureAwait(false);

        logPublisher.Write("Windows location refreshed and protected coordinates were persisted.");
        return Result<GeoCoordinates>.Success(coordinates);
    }

    public ValueTask RefreshScheduleAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return themeTransitionOrchestrator.RefreshAsync(cancellationToken);
    }

    public ValueTask ApplyCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return themeTransitionOrchestrator.ApplyCurrentThemeAsync(cancellationToken);
    }

    public string GetStatusText()
    {
        return themeTransitionOrchestrator.BuildStatusText();
    }

    public string GetTodayScheduleText()
    {
        return themeTransitionOrchestrator.BuildTodayScheduleText();
    }

    public void Dispose()
    {
        themeTransitionOrchestrator.Dispose();
    }

    private void PersistConfigurationState()
    {
        configurationRepository.Save(Config);
        windowsStartupRegistrar.SetEnabled(Config.StartWithWindows, GetExecutablePath());
        themeTransitionOrchestrator.UpdateConfiguration(Config);
    }

    private async ValueTask RefreshScheduleAndThemeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await RefreshScheduleAsync(cancellationToken).ConfigureAwait(false);
        await ApplyCurrentThemeAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string GetExecutablePath()
    {
        return Environment.ProcessPath
        ?? Process.GetCurrentProcess().MainModule?.FileName
        ?? throw new InvalidOperationException("Resolve the executable path before mutating OS registration state.");
    }

    private void ApplyProcessPriority()
    {
        try
        {
            Process.GetCurrentProcess().PriorityClass = Config.UseHighPriority
                ? ProcessPriorityClass.High
                : ProcessPriorityClass.Normal;
        }
        catch (Exception exception)
        {
            logPublisher.Write($"Process priority update failed: {exception.Message}");
        }
    }

    private static AppConfig SanitizeConfiguration(AppConfig configuration)
    {
        int locationPrecisionDecimals = CoordinatePrecisionPolicy.NormalizeDecimals(
            configuration.LocationPrecisionDecimals);

        if (!configuration.IsConfigured)
        {
            return configuration with
            {
                LocationPrecisionDecimals = locationPrecisionDecimals
            };
        }

        GeoCoordinates reducedCoordinates = CoordinatePrecisionPolicy.Reduce(
            new GeoCoordinates
            {
                Latitude = configuration.Latitude,
                Longitude = configuration.Longitude
            },
            locationPrecisionDecimals);

        return configuration with
        {
            Latitude = reducedCoordinates.Latitude,
            Longitude = reducedCoordinates.Longitude,
            LocationPrecisionDecimals = locationPrecisionDecimals
        };
    }
}
