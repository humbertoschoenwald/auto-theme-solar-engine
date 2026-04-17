using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SolarCalculations.Domain;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Themes;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.Themes;

/// <summary>
/// Verifies theme orchestration status text and standard-day application behavior.
/// </summary>
public sealed class ThemeTransitionOrchestratorTests
{
    private static readonly DateOnly BaselineEquinoxDate = new(2026, 3, 29);
    private static readonly TimeZoneInfo BaselineTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        id: "TestMexicoCity",
        baseUtcOffset: TimeSpan.FromHours(-6),
        displayName: "Test Mexico City",
        standardDisplayName: "Test Mexico City");

    /// <summary>
    /// Verifies standard daylight days render the schedule instead of throwing.
    /// </summary>
    [Fact]
    public async Task BuildTodayScheduleText_ReturnsStandardSchedule_WhenDaylightConditionIsStandard()
    {
        using TestContext context = new();
        using ThemeTransitionOrchestrator orchestrator = context.CreateOrchestrator();

        orchestrator.UpdateConfiguration(CreateStandardDayConfiguration());
        await orchestrator.RefreshAsync();

        string scheduleText = orchestrator.BuildTodayScheduleText();
        string statusText = orchestrator.BuildStatusText();

        Assert.Contains("Sunrise", scheduleText, StringComparison.Ordinal);
        Assert.Contains("Sunset", scheduleText, StringComparison.Ordinal);
        Assert.DoesNotContain("AutoThemeSolarEngine", statusText, StringComparison.Ordinal);
        Assert.Contains("Sunrise", statusText, StringComparison.Ordinal);
        Assert.Contains("Sunset", statusText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the optional sunset offset changes the exposed schedule text.
    /// </summary>
    [Fact]
    public async Task BuildTodayScheduleText_UsesAdjustedSunset_WhenExtraMinuteAtSunsetIsEnabled()
    {
        using TestContext context = new();
        SolarSchedule baseSchedule = CreateStandardDaySchedule();
        using ThemeTransitionOrchestrator orchestrator = context.CreateOrchestrator();

        orchestrator.UpdateConfiguration(CreateStandardDayConfiguration());
        await orchestrator.RefreshAsync();

        string scheduleText = orchestrator.BuildTodayScheduleText();

        _ = Assert.NotNull(baseSchedule.SunsetLocal);
        Assert.Contains(baseSchedule.SunsetLocal.Value.AddMinutes(1).ToString("HH:mm"), scheduleText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies standard daylight days can apply a theme without falling into placeholder code.
    /// </summary>
    [Fact]
    public async Task ApplyCurrentThemeAsync_AppliesTheme_WhenDaylightConditionIsStandard()
    {
        using TestContext context = new();
        using ThemeTransitionOrchestrator orchestrator = context.CreateOrchestrator();

        orchestrator.UpdateConfiguration(CreateStandardDayConfiguration());
        await orchestrator.RefreshAsync();
        await orchestrator.ApplyCurrentThemeAsync();

        _ = Assert.Single(context.ThemeMutator.AppliedModes);
    }

    /// <summary>
    /// Verifies the sunset offset keeps light mode active during the extra minute.
    /// </summary>
    [Fact]
    public async Task ApplyCurrentThemeAsync_KeepsLightModeDuringConfiguredSunsetOffset()
    {
        SolarSchedule baseSchedule = CreateStandardDaySchedule();
        _ = Assert.NotNull(baseSchedule.SunsetLocal);
        DateTime momentInsideExtraMinute = AlignWithBaselineDate(baseSchedule.SunsetLocal.Value).AddSeconds(30);

        using TestContext context = new();
        using ThemeTransitionOrchestrator orchestrator = context.CreateOrchestrator(momentInsideExtraMinute);

        orchestrator.UpdateConfiguration(CreateStandardDayConfiguration());
        await orchestrator.RefreshAsync();
        await orchestrator.ApplyCurrentThemeAsync();

        Assert.Equal(ThemeMode.Light, Assert.Single(context.ThemeMutator.AppliedModes));
    }

    /// <summary>
    /// Verifies disabling the sunset offset restores the raw astronomical cutoff.
    /// </summary>
    [Fact]
    public async Task ApplyCurrentThemeAsync_UsesRawSunset_WhenExtraMinuteAtSunsetIsDisabled()
    {
        SolarSchedule baseSchedule = CreateStandardDaySchedule();
        _ = Assert.NotNull(baseSchedule.SunsetLocal);
        DateTime momentAfterRawSunset = AlignWithBaselineDate(baseSchedule.SunsetLocal.Value).AddSeconds(30);

        using TestContext context = new();
        using ThemeTransitionOrchestrator orchestrator = context.CreateOrchestrator(momentAfterRawSunset);

        AppConfig configuration = CreateStandardDayConfiguration() with { AddExtraMinuteAtSunset = false };
        orchestrator.UpdateConfiguration(configuration);
        await orchestrator.RefreshAsync();
        await orchestrator.ApplyCurrentThemeAsync();

        Assert.Equal(ThemeMode.Dark, Assert.Single(context.ThemeMutator.AppliedModes));
    }

    private static AppConfig CreateStandardDayConfiguration()
    {
        return new AppConfig
        {
            Latitude = 19.4326d,
            Longitude = -99.1332d,
            LocationPrecisionDecimals = 3,
            CheckIntervalSeconds = 300,
            IsConfigured = true
        };
    }

    private static SolarSchedule CreateStandardDaySchedule()
    {
        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(19.4326d, -99.1332d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> scheduleResult = SolarPositionEngine.Calculate(
            BaselineEquinoxDate,
            coordinatesResult.Value,
            BaselineTimeZone);

        Assert.True(scheduleResult.IsSuccess, $"{scheduleResult.Error.Code}: {scheduleResult.Error.Description}");
        return scheduleResult.Value;
    }

    private static DateTime AlignWithBaselineDate(DateTime dateTime)
    {
        return BaselineEquinoxDate.ToDateTime(TimeOnly.FromDateTime(dateTime));
    }

    private sealed class TestContext : IDisposable
    {
        private readonly string _directoryPath = Path.Combine(
            Path.GetTempPath(),
            "SolarEngine.Tests",
            Path.GetRandomFileName());

        public TestContext()
        {
            _ = Directory.CreateDirectory(_directoryPath);
        }

        public RecordingThemeMutator ThemeMutator { get; } = new();

        public ThemeTransitionOrchestrator CreateOrchestrator(DateTime? localNow = null)
        {
            StructuredLogPublisher logPublisher = new(Path.Combine(_directoryPath, "AutoThemeSolarEngine.log"));
            return new ThemeTransitionOrchestrator(
                logPublisher,
                new ApplyThemeCommandHandler(ThemeMutator),
                ThemeMutator,
                new AppLocalization(),
                new FixedTimeProvider(
                    localNow ?? new DateTime(2026, 3, 29, 12, 0, 0),
                    BaselineTimeZone));
        }

        public void Dispose()
        {
            if (Directory.Exists(_directoryPath))
            {
                Directory.Delete(_directoryPath, recursive: true);
            }
        }
    }

    private sealed class RecordingThemeMutator : IThemeMutator
    {
        public List<ThemeMode> AppliedModes { get; } = [];

        public Result<ThemeMode> Apply(ThemeMode mode)
        {
            AppliedModes.Add(mode);
            return Result<ThemeMode>.Success(mode);
        }

        public ThemeMode? TryGetCurrentMode()
        {
            return null;
        }
    }

    private sealed class FixedTimeProvider(DateTime localNow, TimeZoneInfo localTimeZone) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => localTimeZone;

        public override DateTimeOffset GetUtcNow()
        {
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localNow, localTimeZone);
            return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
        }
    }
}
