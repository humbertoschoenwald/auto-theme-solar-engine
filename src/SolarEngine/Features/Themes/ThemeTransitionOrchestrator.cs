using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SolarCalculations;
using SolarEngine.Features.SolarCalculations.Domain;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Themes;

internal sealed class ThemeTransitionOrchestrator(
    StructuredLogPublisher logPublisher,
    ApplyThemeCommandHandler applyThemeCommandHandler,
    IThemeMutator themeMutator,
    AppLocalization localization,
    TimeProvider timeProvider) : IDisposable
{
    private const string ScheduleRefreshInvariantDescription = "Refresh the schedule before exposing solar state.";
    private const string WaitingConfigurationKey = "status.waiting_configuration";
    private const string UnknownModeKey = "status.mode_unknown";
    private const string PolarNightTooltipKey = "status.polar_night_tooltip";
    private const string MidnightSunTooltipKey = "status.midnight_sun_tooltip";
    private const string WaitingCoordinatesKey = "status.waiting_coordinates";
    private const string PolarNightWindowKey = "status.polar_night_window";
    private const string MidnightSunWindowKey = "status.midnight_sun_window";
    private const string MissingScheduleLogMessage = "Theme application skipped because no schedule is available.";
    private const string ScheduleTooltipKey = "status.schedule_tooltip";
    private const string ScheduleUnavailableTooltipKey = "status.schedule_unavailable_tooltip";
    private const string ScheduleWindowKey = "status.schedule_window";
    private const string ScheduleUnavailableWindowKey = "status.schedule_unavailable_window";
    private const string TimeDisplayFormat = "HH:mm";
    private const int MinimumCheckIntervalSeconds = 10;
    private const int MaximumCheckIntervalSeconds = 300;
    private const double ExtraSunsetMinuteCount = 1d;

    private readonly SemaphoreSlim _stateGate = new(1, 1);
    private readonly CancellationTokenSource _shutdownCancellationTokenSource = new();

    private AppConfig _configuration = new();
    private SolarSchedule? _todaySchedule;
    private DateOnly _scheduleDate;
    private ThemeMode? _lastAppliedMode;
    private Timer? _timer;
    private bool _isDisposed;

    public event EventHandler<SchedulerStateChangedEventArgs>? StateChanged;

    public void UpdateConfiguration(AppConfig configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ThrowIfDisposed();

        _configuration = configuration;
        TimeSpan checkInterval = ResolveCheckInterval(configuration.CheckIntervalSeconds);
        if (_timer is not null)
        {
            _ = _timer.Change(checkInterval, checkInterval);
        }
    }

    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        TimeSpan checkInterval = ResolveCheckInterval(_configuration.CheckIntervalSeconds);
        if (_timer is not null)
        {
            await _timer.DisposeAsync().ConfigureAwait(false);
        }

        _timer = new Timer(
            static state => ((ThemeTransitionOrchestrator)state!).HandleTick(),
            this,
            checkInterval,
            checkInterval);

        await RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask RefreshAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = _stateGate.Release();
        }
    }

    public SolarSchedule GetTodaySchedule()
    {
        return _todaySchedule ?? throw new UnexpectedStateException(ScheduleRefreshInvariantDescription);
    }

    public async ValueTask ApplyCurrentThemeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();

        await _stateGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await ApplyCurrentThemeCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ = _stateGate.Release();
        }
    }

    public ThemeMode? TryGetCurrentMode()
    {
        return _lastAppliedMode ?? themeMutator.TryGetCurrentMode();
    }

    public string BuildStatusText()
    {
        if (!_configuration.IsConfigured || _todaySchedule is null)
        {
            return localization[WaitingConfigurationKey];
        }

        string modeText = (TryGetCurrentMode()?.ToString() ?? localization[UnknownModeKey]).Trim();

        return _todaySchedule.DaylightCondition switch
        {
            SolarDaylightCondition.PolarNight => localization.Format(PolarNightTooltipKey, modeText),
            SolarDaylightCondition.MidnightSun => localization.Format(MidnightSunTooltipKey, modeText),
            SolarDaylightCondition.Standard => BuildStandardStatusText(modeText),
            _ => BuildStandardStatusText(modeText)
        };
    }

    public string BuildTodayScheduleText()
    {
        return !_configuration.IsConfigured || _todaySchedule is null
            ? localization[WaitingCoordinatesKey]
            : _todaySchedule.DaylightCondition switch
            {
                SolarDaylightCondition.PolarNight => localization[PolarNightWindowKey],
                SolarDaylightCondition.MidnightSun => localization[MidnightSunWindowKey],
                SolarDaylightCondition.Standard => BuildStandardScheduleText(),
                _ => BuildStandardScheduleText()
            };
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _timer?.Dispose();
        _timer = null;
        _stateGate.Dispose();
        _shutdownCancellationTokenSource.Cancel();
        _shutdownCancellationTokenSource.Dispose();
    }

    private void HandleTick()
    {
        if (_isDisposed)
        {
            return;
        }

        _ = ExecuteTimerTickAsync();
    }

    private async Task ExecuteTimerTickAsync()
    {
        try
        {
            await ApplyCurrentThemeAsync(_shutdownCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownCancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (_isDisposed)
        {
        }
        catch (Exception exception)
        {
            logPublisher.Write($"Scheduler tick failed: {exception.Message}");
        }
    }

    private async ValueTask RefreshCoreAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsConfigured)
        {
            _todaySchedule = null;
            _lastAppliedMode = null;
            RaiseStateChanged();
            return;
        }

        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(_configuration.Latitude, _configuration.Longitude);
        if (coordinatesResult.IsFailure)
        {
            _todaySchedule = null;
            _lastAppliedMode = null;
            logPublisher.Write($"Schedule refresh rejected: {coordinatesResult.Error.Description}");
            RaiseStateChanged();
            return;
        }

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        Result<SolarSchedule> scheduleResult = await GetSolarScheduleQueryHandler.HandleAsync(
            new GetSolarScheduleQuery(today, coordinatesResult.Value, timeProvider.LocalTimeZone),
            cancellationToken).ConfigureAwait(false);

        if (scheduleResult.IsFailure)
        {
            _todaySchedule = null;
            logPublisher.Write($"Schedule refresh rejected: {scheduleResult.Error.Description}");
            RaiseStateChanged();
            return;
        }

        _todaySchedule = scheduleResult.Value;
        _scheduleDate = today;
        logPublisher.Write($"Schedule refreshed with daylight condition {_todaySchedule.DaylightCondition}.");
        RaiseStateChanged();
    }

    private async ValueTask ApplyCurrentThemeCoreAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsConfigured)
        {
            _todaySchedule = null;
            _lastAppliedMode = null;
            RaiseStateChanged();
            return;
        }

        await EnsureScheduleCurrentCoreAsync(cancellationToken).ConfigureAwait(false);

        if (_todaySchedule is null)
        {
            logPublisher.Write(MissingScheduleLogMessage);
            RaiseStateChanged();
            return;
        }

        DateTime now = timeProvider.GetLocalNow().DateTime;
        SolarSchedule schedule = _todaySchedule;
        ThemeMode desiredMode = ResolveMode(schedule, now, _configuration);
        ThemeMode? currentMode = themeMutator.TryGetCurrentMode();

        if (_lastAppliedMode == desiredMode && currentMode == desiredMode)
        {
            return;
        }

        Result<ThemeMode> result = applyThemeCommandHandler.Handle(new ApplyThemeCommand(desiredMode));
        if (result.IsSuccess)
        {
            _lastAppliedMode = result.Value;
            logPublisher.Write($"Theme applied: {result.Value}.");
        }
        else
        {
            logPublisher.Write($"Theme application rejected: {result.Error.Description}");
        }

        RaiseStateChanged();
    }

    private async ValueTask EnsureScheduleCurrentCoreAsync(CancellationToken cancellationToken)
    {
        if (!_configuration.IsConfigured)
        {
            return;
        }

        DateOnly today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);
        if (_todaySchedule is null || today != _scheduleDate)
        {
            await RefreshCoreAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, new SchedulerStateChangedEventArgs(BuildStatusText()));
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private static TimeSpan ResolveCheckInterval(int checkIntervalSeconds)
    {
        return TimeSpan.FromSeconds(
            Math.Clamp(
                checkIntervalSeconds,
                MinimumCheckIntervalSeconds,
                MaximumCheckIntervalSeconds));
    }

    private static ThemeMode ResolveMode(SolarSchedule schedule, DateTime now, AppConfig configuration)
    {
        DateTime? sunsetThreshold = ResolveEffectiveSunsetLocal(schedule, configuration);

        return schedule.DaylightCondition switch
        {
            SolarDaylightCondition.PolarNight => ThemeMode.Dark,
            SolarDaylightCondition.MidnightSun => ThemeMode.Light,
            _ when schedule.SunriseLocal is not null
                && sunsetThreshold is not null
                && now >= schedule.SunriseLocal.Value
                && now < sunsetThreshold.Value => ThemeMode.Light,
            SolarDaylightCondition.Standard => ThemeMode.Dark,
            _ => ThemeMode.Dark
        };
    }

    private string BuildStandardStatusText(string modeText)
    {
        return _todaySchedule is { SunriseLocal: DateTime sunrise }
            && ResolveEffectiveSunsetLocal(_todaySchedule, _configuration) is DateTime sunset
            ? localization.Format(
                ScheduleTooltipKey,
                modeText,
                sunrise.ToString(TimeDisplayFormat),
                sunset.ToString(TimeDisplayFormat))
            : localization.Format(ScheduleUnavailableTooltipKey, modeText);
    }

    private string BuildStandardScheduleText()
    {
        return _todaySchedule is { SunriseLocal: DateTime sunrise }
            && ResolveEffectiveSunsetLocal(_todaySchedule, _configuration) is DateTime sunset
            ? localization.Format(ScheduleWindowKey, sunrise.ToString(TimeDisplayFormat), sunset.ToString(TimeDisplayFormat))
            : localization[ScheduleUnavailableWindowKey];
    }

    private static DateTime? ResolveEffectiveSunsetLocal(SolarSchedule schedule, AppConfig configuration)
    {
        return schedule.SunsetLocal is not DateTime sunset
            ? null
            : configuration.AddExtraMinuteAtSunset
            ? sunset.AddMinutes(ExtraSunsetMinuteCount)
            : sunset;
    }
}
