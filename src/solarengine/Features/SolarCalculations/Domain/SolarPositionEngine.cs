using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SolarCalculations.Domain;

internal static class SolarPositionEngine
{
    private const double Zenith = 90.833d;
    private const double DegreesPerHour = 15d;
    private const double HoursPerDay = 24d;
    private const double DegreesPerRadian = 180d / Math.PI;
    private const double RadiansPerDegree = Math.PI / 180d;
    private const double Epsilon = 1e-12d;
    private const long TicksPerDay = TimeSpan.TicksPerDay;

    public static Result<SolarSchedule> Calculate(DateOnly date, GeoCoordinates coordinates)
    {
        return Calculate(date, coordinates, TimeZoneInfo.Local);
    }

    internal static Result<SolarSchedule> Calculate(
        DateOnly date,
        GeoCoordinates coordinates,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);

        if (coordinates is null)
        {
            return Result<SolarSchedule>.Failure(
                new Error(
                    "solar.schedule.coordinates_required",
                    "Require coordinates before evaluating solar events."));
        }

        Error validationError = ValidateCoordinates(coordinates);
        if (validationError != Error.None)
        {
            return Result<SolarSchedule>.Failure(validationError);
        }

        SolarEventResult sunriseResult = CalculateSolarEvent(date, coordinates, isSunrise: true);
        if (sunriseResult.Error != Error.None)
        {
            return Result<SolarSchedule>.Failure(sunriseResult.Error);
        }

        SolarEventResult sunsetResult = CalculateSolarEvent(date, coordinates, isSunrise: false);
        if (sunsetResult.Error != Error.None)
        {
            return Result<SolarSchedule>.Failure(sunsetResult.Error);
        }

        SolarDaylightCondition daylightCondition = ResolveDaylightCondition(
            sunriseResult.DaylightCondition,
            sunsetResult.DaylightCondition);

        DateTime? sunriseLocal = sunriseResult.UtcHours is double sunriseUtcHours
            ? ConvertUtcHoursToLocalDateTime(date, sunriseUtcHours, timeZone)
            : null;

        DateTime? sunsetLocal = sunsetResult.UtcHours is double sunsetUtcHours
            ? ConvertUtcHoursToLocalDateTime(date, sunsetUtcHours, timeZone)
            : null;

        return Result<SolarSchedule>.Success(
            new SolarSchedule(date, sunriseLocal, sunsetLocal, daylightCondition));
    }

    private static SolarEventResult CalculateSolarEvent(DateOnly date, GeoCoordinates coordinates, bool isSunrise)
    {
        int dayOfYear = date.DayOfYear;
        double longitudeHour = coordinates.Longitude / DegreesPerHour;
        double approximateTime = isSunrise
            ? dayOfYear + ((6d - longitudeHour) / HoursPerDay)
            : dayOfYear + ((18d - longitudeHour) / HoursPerDay);

        double meanAnomaly = (0.9856d * approximateTime) - 3.289d;
        double trueLongitude = NormalizeDegrees(
            meanAnomaly
            + (1.916d * Math.Sin(ToRadians(meanAnomaly)))
            + (0.020d * Math.Sin(ToRadians(2d * meanAnomaly)))
            + 282.634d);

        double rightAscension = NormalizeDegrees(ToDegrees(Math.Atan(0.91764d * Math.Tan(ToRadians(trueLongitude)))));
        rightAscension = AdjustQuadrant(rightAscension, trueLongitude) / DegreesPerHour;

        double sinDeclination = 0.39782d * Math.Sin(ToRadians(trueLongitude));
        double cosDeclination = Math.Cos(Math.Asin(sinDeclination));
        double latitudeRadians = ToRadians(coordinates.Latitude);
        double sinLatitude = Math.Sin(latitudeRadians);
        double cosLatitude = Math.Cos(latitudeRadians);
        double denominator = cosDeclination * cosLatitude;
        double numerator = Math.Cos(ToRadians(Zenith)) - (sinDeclination * sinLatitude);

        if (Math.Abs(denominator) <= Epsilon)
        {
            SolarDaylightCondition poleCondition = numerator <= 0d
                ? SolarDaylightCondition.MidnightSun
                : SolarDaylightCondition.PolarNight;

            return new SolarEventResult(null, poleCondition, Error.None);
        }

        double cosHourAngle = numerator / denominator;

        if (cosHourAngle > 1d)
        {
            return new SolarEventResult(null, SolarDaylightCondition.PolarNight, Error.None);
        }

        if (cosHourAngle < -1d)
        {
            return new SolarEventResult(null, SolarDaylightCondition.MidnightSun, Error.None);
        }

        double localHourAngle = isSunrise
            ? 360d - ToDegrees(Math.Acos(cosHourAngle))
            : ToDegrees(Math.Acos(cosHourAngle));

        double localMeanTime = NormalizeHours(
            (localHourAngle / DegreesPerHour)
            + rightAscension
            - (0.06571d * approximateTime)
            - 6.622d);

        double utcHours = NormalizeHours(localMeanTime - longitudeHour);

        return new SolarEventResult(utcHours, SolarDaylightCondition.Standard, Error.None);
    }

    private static Error ValidateCoordinates(GeoCoordinates coordinates)
    {
        return !double.IsFinite(coordinates.Latitude)
            ? new Error(
                "solar.schedule.latitude_non_finite",
                "Reject non-finite latitude values to preserve deterministic solar calculations.")
            : !double.IsFinite(coordinates.Longitude)
            ? new Error(
                "solar.schedule.longitude_non_finite",
                "Reject non-finite longitude values to preserve deterministic solar calculations.")
            : coordinates.Latitude is < -90d or > 90d
            ? new Error(
                "solar.schedule.latitude_out_of_range",
                "Constrain latitude to the astronomical domain.")
            : coordinates.Longitude is < -180d or > 180d
            ? new Error(
                "solar.schedule.longitude_out_of_range",
                "Constrain longitude to the astronomical domain.")
            : Error.None;
    }

    private static SolarDaylightCondition ResolveDaylightCondition(
        SolarDaylightCondition sunriseCondition,
        SolarDaylightCondition sunsetCondition)
    {
        return sunriseCondition is not SolarDaylightCondition.Standard
                ? sunriseCondition
                : sunsetCondition;
    }

    private static DateTime ConvertUtcHoursToLocalDateTime(
        DateOnly date,
        double utcHours,
        TimeZoneInfo timeZone)
    {
        long ticks = (long)Math.Round(
            utcHours * TimeSpan.TicksPerHour,
            MidpointRounding.AwayFromZero);

        long normalizedTicks = ((ticks % TicksPerDay) + TicksPerDay) % TicksPerDay;
        DateTime utcMidnight = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        DateTime utcDateTime = utcMidnight.AddTicks(normalizedTicks);
        DateOnly targetLocalDate = date;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
            DateOnly convertedLocalDate = DateOnly.FromDateTime(localDateTime);

            if (convertedLocalDate == targetLocalDate)
            {
                return localDateTime;
            }

            utcDateTime = convertedLocalDate < targetLocalDate
                ? utcDateTime.AddDays(1)
                : utcDateTime.AddDays(-1);
        }

        throw new InvalidOperationException("Resolve a solar event timestamp that lands on the requested local date.");
    }

    private static double NormalizeDegrees(double value)
    {
        double result = value % 360d;
        return result < 0d ? result + 360d : result;
    }

    private static double NormalizeHours(double value)
    {
        double result = value % HoursPerDay;
        return result < 0d ? result + HoursPerDay : result;
    }

    private static double AdjustQuadrant(double rightAscension, double trueLongitude)
    {
        double longitudeQuadrant = Math.Floor(trueLongitude / 90d) * 90d;
        double rightAscensionQuadrant = Math.Floor(rightAscension / 90d) * 90d;

        return rightAscension + (longitudeQuadrant - rightAscensionQuadrant);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * RadiansPerDegree;
    }

    private static double ToDegrees(double radians)
    {
        return radians * DegreesPerRadian;
    }

    private readonly record struct SolarEventResult(
        double? UtcHours,
        SolarDaylightCondition DaylightCondition,
        Error Error);
}
