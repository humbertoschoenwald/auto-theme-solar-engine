using SolarEngine.Features.Locations.Domain;
using SolarEngine.Features.SolarCalculations.Domain;
using SolarEngine.Shared.Core;
using Xunit;

namespace SolarEngine.Tests.Features.SolarCalculations;

/// <summary>
/// Verifies solar schedule calculations across ordinary, polar, and offset-sensitive days.
/// </summary>
public sealed class SolarPositionEngineTests
{
    private static readonly DateOnly BaselineEquinoxDate = new(2026, 3, 29);

    /// <summary>
    /// Verifies standard daylight calculations produce sunrise before sunset near UTC.
    /// </summary>
    [Fact]
    public void Calculate_ProducesSunriseBeforeSunset_ForNearUtcCoordinates()
    {
        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(51.5074d, -0.1278d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> scheduleResult =
            SolarPositionEngine.Calculate(BaselineEquinoxDate, coordinatesResult.Value, TimeZoneInfo.Utc);

        Assert.True(
            scheduleResult.IsSuccess,
            $"{scheduleResult.Error.Code}: {scheduleResult.Error.Description}");

        SolarSchedule schedule = scheduleResult.Value;

        Assert.Equal(SolarDaylightCondition.Standard, schedule.DaylightCondition);
        _ = Assert.NotNull(schedule.SunriseLocal);
        _ = Assert.NotNull(schedule.SunsetLocal);
        Assert.True(
            schedule.SunriseLocal < schedule.SunsetLocal,
            $"Sunrise={schedule.SunriseLocal:O}; Sunset={schedule.SunsetLocal:O}");

        TimeSpan daylight = schedule.SunsetLocal.Value - schedule.SunriseLocal.Value;
        Assert.InRange(daylight.TotalHours, 11d, 13d);
    }

    /// <summary>
    /// Verifies invalid coordinate inputs fail before schedule calculation.
    /// </summary>
    [Theory]
    [InlineData(-91d, 0d)]
    [InlineData(91d, 0d)]
    [InlineData(0d, -181d)]
    [InlineData(0d, 181d)]
    [InlineData(double.NaN, 0d)]
    [InlineData(0d, double.NaN)]
    [InlineData(double.PositiveInfinity, 0d)]
    [InlineData(0d, double.NegativeInfinity)]
    public void Calculate_RejectsInvalidAndNonFiniteCoordinates(double latitude, double longitude)
    {
        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(latitude, longitude);
        Assert.True(coordinatesResult.IsFailure);
    }

    /// <summary>
    /// Verifies winter polar coordinates report no sunrise event.
    /// </summary>
    [Fact]
    public void Calculate_UsesPolarNightCondition_WhenSunNeverRises()
    {
        DateOnly winterSolstice = new(2026, 12, 21);

        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(78.2232d, 15.6469d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> scheduleResult =
            SolarPositionEngine.Calculate(winterSolstice, coordinatesResult.Value);

        Assert.True(scheduleResult.IsSuccess);

        SolarSchedule schedule = scheduleResult.Value;

        Assert.Equal(SolarDaylightCondition.PolarNight, schedule.DaylightCondition);
        Assert.Null(schedule.SunriseLocal);
        Assert.Null(schedule.SunsetLocal);
    }

    /// <summary>
    /// Verifies summer polar coordinates report no sunset event.
    /// </summary>
    [Fact]
    public void Calculate_UsesMidnightSunCondition_WhenSunNeverSets()
    {
        DateOnly summerSolstice = new(2026, 6, 21);

        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(78.2232d, 15.6469d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> scheduleResult =
            SolarPositionEngine.Calculate(summerSolstice, coordinatesResult.Value);

        Assert.True(scheduleResult.IsSuccess);

        SolarSchedule schedule = scheduleResult.Value;

        Assert.Equal(SolarDaylightCondition.MidnightSun, schedule.DaylightCondition);
        Assert.Null(schedule.SunriseLocal);
        Assert.Null(schedule.SunsetLocal);
    }

    /// <summary>
    /// Verifies negative UTC offsets keep sunset on the requested local date.
    /// </summary>
    [Fact]
    public void Calculate_KeepsSunsetOnRequestedLocalDate_ForNegativeUtcOffsets()
    {
        DateOnly date = new(2026, 3, 31);
        TimeZoneInfo mexicoLikeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");

        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(19.4471d, -99.1809d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> scheduleResult =
            SolarPositionEngine.Calculate(date, coordinatesResult.Value, mexicoLikeTimeZone);

        Assert.True(
            scheduleResult.IsSuccess,
            $"{scheduleResult.Error.Code}: {scheduleResult.Error.Description}");

        SolarSchedule schedule = scheduleResult.Value;

        _ = Assert.NotNull(schedule.SunriseLocal);
        _ = Assert.NotNull(schedule.SunsetLocal);
        Assert.Equal(date, DateOnly.FromDateTime(schedule.SunriseLocal.Value));
        Assert.Equal(date, DateOnly.FromDateTime(schedule.SunsetLocal.Value));
        Assert.True(schedule.SunriseLocal < schedule.SunsetLocal);
        Assert.InRange(schedule.SunriseLocal.Value.TimeOfDay, TimeSpan.FromHours(6), TimeSpan.FromHours(7));
        Assert.InRange(schedule.SunsetLocal.Value.TimeOfDay, TimeSpan.FromHours(18), TimeSpan.FromHours(19.5));
    }

    /// <summary>
    /// Verifies daylight saving boundaries keep solar events on the requested local date.
    /// </summary>
    [Theory]
    [InlineData(2026, 3, 8)]
    [InlineData(2026, 11, 1)]
    public void Calculate_KeepsSolarEventsOnRequestedLocalDate_AcrossDstTransitions(int year, int month, int day)
    {
        DateOnly date = new(year, month, day);
        TimeZoneInfo easternTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        Result<GeoCoordinates> coordinatesResult = GeoCoordinates.Create(40.7128d, -74.0060d);
        Assert.True(coordinatesResult.IsSuccess);

        Result<SolarSchedule> scheduleResult =
            SolarPositionEngine.Calculate(date, coordinatesResult.Value, easternTimeZone);

        Assert.True(
            scheduleResult.IsSuccess,
            $"{scheduleResult.Error.Code}: {scheduleResult.Error.Description}");

        SolarSchedule schedule = scheduleResult.Value;

        _ = Assert.NotNull(schedule.SunriseLocal);
        _ = Assert.NotNull(schedule.SunsetLocal);
        Assert.Equal(date, DateOnly.FromDateTime(schedule.SunriseLocal.Value));
        Assert.Equal(date, DateOnly.FromDateTime(schedule.SunsetLocal.Value));
        Assert.True(schedule.SunriseLocal < schedule.SunsetLocal);
        Assert.InRange(
            (schedule.SunsetLocal.Value - schedule.SunriseLocal.Value).TotalHours,
            9d,
            13d);
    }
}
