using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Themes;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.Themes;

/// <summary>
/// Verifies theme orchestration status text and standard-day application behavior.
/// </summary>
public sealed class ThemeTransitionOrchestratorTests
{
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
        Assert.Contains("Auto Theme Solar Engine", statusText, StringComparison.Ordinal);
        Assert.Contains("Sunrise", statusText, StringComparison.Ordinal);
        Assert.Contains("Sunset", statusText, StringComparison.Ordinal);
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

    private static AppConfig CreateStandardDayConfiguration()
    {
        return new AppConfig
        {
            Latitude = 51.5074d,
            Longitude = -0.1278d,
            LocationPrecisionDecimals = 3,
            CheckIntervalSeconds = 300,
            IsConfigured = true
        };
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

        public ThemeTransitionOrchestrator CreateOrchestrator()
        {
            StructuredLogPublisher logPublisher = new(Path.Combine(_directoryPath, "AutoThemeSolarEngine.log"));
            return new ThemeTransitionOrchestrator(
                logPublisher,
                new ApplyThemeCommandHandler(ThemeMutator),
                ThemeMutator,
                new FixedTimeProvider(new DateTimeOffset(2026, 3, 29, 12, 0, 0, TimeSpan.Zero)));
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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
