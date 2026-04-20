using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SolarEngine.Features.Locations;
using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SolarCalculations;
using SolarEngine.Features.SystemHost;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Themes;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Features.Updates;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared;
using SolarEngine.Shared.Core;
using SolarEngine.UI;

namespace SolarEngine;

internal sealed class NativeApplication : IDisposable
{
    private const string AppName = AppIdentity.RuntimeName;
    private const string SingleInstanceMutexName = @"Local\AutoThemeSolarEngine.SingleInstance";
    private static readonly TimeSpan s_automaticUpdateCheckInterval = TimeSpan.FromHours(4);

    private bool _disposed;
    private Mutex? _instanceMutex;
    private bool _ownsMutex;
    private ServiceProvider? _serviceProvider;
    private ApplicationLifecycleOrchestrator? _applicationLifecycleOrchestrator;
    private ThemeTransitionOrchestrator? _themeTransitionOrchestrator;
    private StructuredLogPublisher? _structuredLogPublisher;
    private AppLocalization? _localization;
    private UpdateCoordinator? _updateCoordinator;
    private TimeProvider? _timeProvider;
    private CancellationTokenSource? _applicationLifetimeCancellationTokenSource;
    private TrayIconHost? _trayIconHost;
    private SettingsWindow? _settingsWindow;
    private Task? _updateMonitoringTask;
    private int _refreshInProgress;

    public int Run()
    {
        AppPaths appPaths = new();
        _applicationLifetimeCancellationTokenSource = new CancellationTokenSource();

        AppDomain.CurrentDomain.UnhandledException += HandleUnhandledException;
        TaskScheduler.UnobservedTaskException += HandleUnobservedTaskException;

        try
        {
            if (!TryAcquireSingleInstance())
            {
                ShowMessage(
                    _localization?["app.already_running"]
                    ?? $"{AppIdentity.RuntimeName} is already running in the notification area.",
                    NativeInterop.MB_ICONINFORMATION);

                return 0;
            }

            InitializeServices();

            _trayIconHost = new TrayIconHost(AppName, _localization!);
            _trayIconHost.OpenRequested += ShowSettingsWindow;
            _trayIconHost.ApplyNowRequested += HandleApplyNowRequested;
            _trayIconHost.RecalculateTodayRequested += HandleRecalculateTodayRequested;
            _trayIconHost.ExitRequested += ExitApplication;
            _trayIconHost.PowerResumed += HandlePowerResumed;
            _trayIconHost.ClockChanged += HandleClockChanged;
            _trayIconHost.SessionActivated += HandleSessionActivated;
            _trayIconHost.Create();

            _themeTransitionOrchestrator!.StateChanged += HandleStateChanged;

            _applicationLifecycleOrchestrator!.Initialize();
            _updateCoordinator!.EnsureInstallationReady();
            _settingsWindow = new SettingsWindow(_applicationLifecycleOrchestrator, _localization!, _updateCoordinator!);

            _applicationLifecycleOrchestrator
                .StartAsync(GetApplicationLifetimeCancellationToken())
                .AsTask()
                .GetAwaiter()
                .GetResult();

            if (_applicationLifecycleOrchestrator.Config.UseWindowsLocation)
            {
                StartBackgroundRefresh(showErrors: false);
            }

            StartUpdateMonitoring();

            _trayIconHost.SetTooltip(_applicationLifecycleOrchestrator.GetStatusText());

            if (!_applicationLifecycleOrchestrator.Config.IsConfigured
                || !_applicationLifecycleOrchestrator.Config.StartMinimized)
            {
                ShowSettingsWindow();
            }

            return NativeInterop.RunMessageLoop();
        }
        catch (OperationCanceledException) when (IsApplicationShuttingDown())
        {
            return 0;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or UnexpectedStateException
            or Win32Exception)
        {
            HandleStartupFailure(appPaths, exception, $"{AppIdentity.RuntimeName} failed during startup.");
            return -1;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        CancelApplicationLifetime();

        AppDomain.CurrentDomain.UnhandledException -= HandleUnhandledException;
        TaskScheduler.UnobservedTaskException -= HandleUnobservedTaskException;

        ThemeTransitionOrchestrator? themeTransitionOrchestrator = _themeTransitionOrchestrator;
        themeTransitionOrchestrator?.StateChanged -= HandleStateChanged;

        if (_trayIconHost is not null)
        {
            _trayIconHost.OpenRequested -= ShowSettingsWindow;
            _trayIconHost.ApplyNowRequested -= HandleApplyNowRequested;
            _trayIconHost.RecalculateTodayRequested -= HandleRecalculateTodayRequested;
            _trayIconHost.ExitRequested -= ExitApplication;
            _trayIconHost.PowerResumed -= HandlePowerResumed;
            _trayIconHost.ClockChanged -= HandleClockChanged;
            _trayIconHost.SessionActivated -= HandleSessionActivated;
            _trayIconHost.Close();
            _trayIconHost = null;
        }

        _settingsWindow?.Close();
        _settingsWindow = null;
        themeTransitionOrchestrator?.Dispose();
        _applicationLifecycleOrchestrator?.Dispose();
        _applicationLifecycleOrchestrator = null;
        _themeTransitionOrchestrator = null;

        DisposeServiceProvider();

        if (_instanceMutex is not null)
        {
            if (_ownsMutex)
            {
                _instanceMutex.ReleaseMutex();
            }

            _instanceMutex.Dispose();
            _instanceMutex = null;
        }

        _applicationLifetimeCancellationTokenSource?.Dispose();
        _applicationLifetimeCancellationTokenSource = null;
    }

    private void InitializeServices()
    {
        ServiceCollection services = new();
        _ = services
            .AddLocationsFeature()
            .AddSolarCalculationsFeature()
            .AddThemesFeature()
            .AddSystemHostFeature()
            .AddUpdatesFeature();

        _serviceProvider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        _structuredLogPublisher = _serviceProvider.GetRequiredService<StructuredLogPublisher>();
        _localization = _serviceProvider.GetRequiredService<AppLocalization>();
        _applicationLifecycleOrchestrator =
            _serviceProvider.GetRequiredService<ApplicationLifecycleOrchestrator>();
        _themeTransitionOrchestrator =
            _serviceProvider.GetRequiredService<ThemeTransitionOrchestrator>();
        _updateCoordinator = _serviceProvider.GetRequiredService<UpdateCoordinator>();
        _timeProvider = _serviceProvider.GetRequiredService<TimeProvider>();
    }

    private bool TryAcquireSingleInstance()
    {
        _instanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out bool createdNew);
        _ownsMutex = createdNew;
        return createdNew;
    }

    private void ShowSettingsWindow()
    {
        _settingsWindow?.ShowFromTray();
    }

    private void HandleApplyNowRequested()
    {
        StartBackgroundRefresh(showErrors: true);
    }

    private void HandleRecalculateTodayRequested()
    {
        StartBackgroundRefresh(showErrors: true);
    }

    private void HandlePowerResumed()
    {
        StartBackgroundRefresh(showErrors: false);
    }

    private void HandleSessionActivated()
    {
        StartBackgroundRefresh(showErrors: false);
    }

    private void HandleClockChanged()
    {
        StartBackgroundRefresh(showErrors: false);
    }

    private void StartUpdateMonitoring()
    {
        if (_applicationLifecycleOrchestrator is null
            || _updateCoordinator is null
            || _timeProvider is null
            || _updateMonitoringTask is not null)
        {
            return;
        }

        _updateMonitoringTask = MonitorUpdatesAsync();
    }

    private void HandleStateChanged(object? sender, SchedulerStateChangedEventArgs eventArgs)
    {
        _trayIconHost?.SetTooltip(eventArgs.Tooltip);
        _settingsWindow?.RequestRefresh();
    }

    private void ExitApplication()
    {
        CancelApplicationLifetime();
        _settingsWindow?.Close();
        _settingsWindow = null;
        _trayIconHost?.Close();
        _trayIconHost = null;
    }

    private void StartBackgroundRefresh(bool showErrors)
    {
        if (_applicationLifecycleOrchestrator is null
            || Interlocked.CompareExchange(ref _refreshInProgress, 1, 0) != 0)
        {
            return;
        }

        _ = RefreshLocationAndThemeAsync(showErrors);
    }

    private async Task RefreshLocationAndThemeAsync(bool showErrors)
    {
        try
        {
            ApplicationLifecycleOrchestrator? applicationLifecycleOrchestrator = _applicationLifecycleOrchestrator;
            if (applicationLifecycleOrchestrator is null)
            {
                return;
            }

            CancellationToken cancellationToken = GetApplicationLifetimeCancellationToken();
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (applicationLifecycleOrchestrator.Config.UseWindowsLocation)
            {
                Result<GeoCoordinates> refreshResult =
                    await applicationLifecycleOrchestrator
                        .RefreshCoordinatesFromWindowsAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (refreshResult.IsFailure)
                {
                    _structuredLogPublisher?.Write(
                        $"Windows location refresh skipped: {refreshResult.Error.Description}");
                }
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            await applicationLifecycleOrchestrator
                .RefreshScheduleAsync(cancellationToken)
                .ConfigureAwait(false);

            await applicationLifecycleOrchestrator
                .ApplyCurrentThemeAsync(cancellationToken)
                .ConfigureAwait(false);

            _trayIconHost?.SetTooltip(applicationLifecycleOrchestrator.GetStatusText());
            _settingsWindow?.RequestRefresh();
        }
        catch (OperationCanceledException) when (IsApplicationShuttingDown())
        {
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or UnexpectedStateException
            or Win32Exception)
        {
            _structuredLogPublisher?.Write($"Runtime refresh failed: {exception}");

            if (showErrors)
            {
                ShowMessage(exception.Message, NativeInterop.MB_ICONERROR);
            }
        }
        finally
        {
            _ = Interlocked.Exchange(ref _refreshInProgress, 0);
        }
    }

    private async Task MonitorUpdatesAsync()
    {
        try
        {
            if (await RefreshUpdateSnapshotAndMaybeApplyAsync().ConfigureAwait(false))
            {
                return;
            }

            using PeriodicTimer timer = new(s_automaticUpdateCheckInterval, _timeProvider!);
            while (await timer.WaitForNextTickAsync(GetApplicationLifetimeCancellationToken()).ConfigureAwait(false))
            {
                if (await RefreshUpdateSnapshotAndMaybeApplyAsync().ConfigureAwait(false))
                {
                    return;
                }
            }
        }
        catch (OperationCanceledException) when (IsApplicationShuttingDown())
        {
        }
    }

    private async Task<bool> RefreshUpdateSnapshotAndMaybeApplyAsync()
    {
        try
        {
            ApplicationLifecycleOrchestrator? applicationLifecycleOrchestrator = _applicationLifecycleOrchestrator;
            UpdateCoordinator? updateCoordinator = _updateCoordinator;
            if (applicationLifecycleOrchestrator is null || updateCoordinator is null)
            {
                return false;
            }

            CancellationToken cancellationToken = GetApplicationLifetimeCancellationToken();
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            UpdateStatusSnapshot updateSnapshot = await updateCoordinator
                .CheckForUpdatesAsync(cancellationToken)
                .ConfigureAwait(false);
            _settingsWindow?.RequestRefresh();

            if (!applicationLifecycleOrchestrator.Config.AutomaticUpdatesEnabled
                || !updateSnapshot.IsUpdateAvailable
                || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            bool updatePrepared = await updateCoordinator
                .PrepareAndLaunchUpdateAsync(applicationLifecycleOrchestrator.Config, cancellationToken)
                .ConfigureAwait(false);

            if (!updatePrepared || cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            _structuredLogPublisher?.Write("Automatic update prepared. Exiting current process to apply the new executable.");
            ExitApplication();
            return true;
        }
        catch (OperationCanceledException) when (IsApplicationShuttingDown())
        {
            return false;
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or IOException
            or UnauthorizedAccessException
            or UnexpectedStateException
            or Win32Exception)
        {
            _updateCoordinator?.RecordCheckFailure(exception.Message);
            _settingsWindow?.RequestRefresh();
            _structuredLogPublisher?.Write($"Automatic update check failed: {exception}");
            return false;
        }
    }

    private void HandleUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
    {
        if (eventArgs.ExceptionObject is Exception exception)
        {
            _structuredLogPublisher?.Write($"Unhandled exception: {exception}");
        }
    }

    private void HandleUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
    {
        _structuredLogPublisher?.Write($"Unobserved task exception: {eventArgs.Exception}");
        eventArgs.SetObserved();
    }

    private void CancelApplicationLifetime()
    {
        if (_applicationLifetimeCancellationTokenSource is { IsCancellationRequested: false })
        {
            _applicationLifetimeCancellationTokenSource.Cancel();
        }
    }

    private CancellationToken GetApplicationLifetimeCancellationToken()
    {
        return _applicationLifetimeCancellationTokenSource?.Token ?? CancellationToken.None;
    }

    private bool IsApplicationShuttingDown()
    {
        return _applicationLifetimeCancellationTokenSource is { IsCancellationRequested: true };
    }

    private void DisposeServiceProvider()
    {
        if (_serviceProvider is null)
        {
            return;
        }

        _serviceProvider.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _serviceProvider = null;
    }

    private void HandleStartupFailure(AppPaths appPaths, Exception exception, string headline)
    {
        WriteEmergencyCrashLog(appPaths.LogPath, exception);
        _structuredLogPublisher?.Write($"Fatal startup error: {exception}");

        string message =
            $"{headline}{Environment.NewLine}{Environment.NewLine}{exception.Message}"
            + $"{Environment.NewLine}{Environment.NewLine}See log:{Environment.NewLine}{appPaths.LogPath}";

        _ = NativeInterop.MessageBox(
            nint.Zero,
            message,
            AppName,
            NativeInterop.MB_OK | NativeInterop.MB_ICONERROR);
    }

    private static void WriteEmergencyCrashLog(string logPath, Exception exception)
    {
        string? directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        File.AppendAllText(
            logPath,
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} | Fatal startup error | {exception}{Environment.NewLine}");
    }

    private static void ShowMessage(string message, int iconFlags)
    {
        _ = NativeInterop.MessageBox(
            nint.Zero,
            message,
            AppName,
            NativeInterop.MB_OK | iconFlags);
    }
}
