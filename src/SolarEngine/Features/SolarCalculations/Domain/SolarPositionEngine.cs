// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.SolarCalculations.Domain;

internal static class SolarPositionEngine
{
    private const string CoordinatesRequiredCode = "solar.schedule.coordinates_required";
    private const string CoordinatesRequiredDescription = "Require coordinates before evaluating solar events.";
    private const string LatitudeNonFiniteCode = "solar.schedule.latitude_non_finite";
    private const string LatitudeNonFiniteDescription = "Reject non-finite latitude values to preserve deterministic solar calculations.";
    private const string LongitudeNonFiniteCode = "solar.schedule.longitude_non_finite";
    private const string LongitudeNonFiniteDescription = "Reject non-finite longitude values to preserve deterministic solar calculations.";
    private const string LatitudeOutOfRangeCode = "solar.schedule.latitude_out_of_range";
    private const string LatitudeOutOfRangeDescription = "Constrain latitude to the astronomical domain.";
    private const string LongitudeOutOfRangeCode = "solar.schedule.longitude_out_of_range";
    private const string LongitudeOutOfRangeDescription = "Constrain longitude to the astronomical domain.";
    private const string LocalTimestampInvariantDescription = "Resolve a solar event timestamp that lands on the requested local date.";
    private const double Zenith = 90.833d;
    private const double DegreesPerHour = 15d;
    private const double HoursPerDay = 24d;
    private const double Epsilon = 1e-12d;

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
                    CoordinatesRequiredCode,
                    CoordinatesRequiredDescription));
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
        // We keep the NOAA/USNO-style approximation because this product only needs
        // schedule-grade sunrise and sunset accuracy, not the heavier NREL SPA model
        // used for solar-radiation hardware and calibration workflows.
        double approximateTime = CalculateApproximateTime();
        double meanAnomaly = CalculateMeanAnomaly(approximateTime);
        double trueLongitude = CalculateTrueLongitude(meanAnomaly);
        double rightAscension = CalculateRightAscension(trueLongitude);
        rightAscension = AdjustQuadrant(rightAscension, trueLongitude) / DegreesPerHour;

        double sinDeclination = CalculateSinDeclination(trueLongitude);
        double cosDeclination = Math.Cos(Math.Asin(sinDeclination));
        double latitudeRadians = coordinates.Latitude.ToRadians();
        double sinLatitude = Math.Sin(latitudeRadians);
        double cosLatitude = Math.Cos(latitudeRadians);
        double denominator = cosDeclination * cosLatitude;
        double numerator = Math.Cos(Zenith.ToRadians()) - (sinDeclination * sinLatitude);

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

        double localHourAngle = CalculateLocalHourAngle(cosHourAngle);
        double localMeanTime = CalculateLocalMeanTime(localHourAngle, rightAscension, approximateTime);
        double utcHours = (localMeanTime - longitudeHour).NormalizeHours();

        return new SolarEventResult(utcHours, SolarDaylightCondition.Standard, Error.None);

        double CalculateApproximateTime()
        {
            double baseHour = isSunrise ? 6d : 18d;
            return dayOfYear + ((baseHour - longitudeHour) / HoursPerDay);
        }

        double CalculateMeanAnomaly(double eventApproximateTime)
        {
            const double DailyAdvanceDegrees = 0.9856d;
            const double EpochOffsetDegrees = 3.289d;

            return (DailyAdvanceDegrees * eventApproximateTime) - EpochOffsetDegrees;
        }

        double CalculateTrueLongitude(double anomaly)
        {
            const double PrimaryCenterCorrectionDegrees = 1.916d;
            const double SecondaryCenterCorrectionDegrees = 0.020d;
            const double PerihelionLongitudeDegrees = 282.634d;

            return (
                anomaly
                + (PrimaryCenterCorrectionDegrees * Math.Sin(anomaly.ToRadians()))
                + (SecondaryCenterCorrectionDegrees * Math.Sin((2d * anomaly).ToRadians()))
                + PerihelionLongitudeDegrees).NormalizeDegrees();
        }

        double CalculateRightAscension(double longitude)
        {
            const double EclipticProjectionFactor = 0.91764d;

            return Math.Atan(EclipticProjectionFactor * Math.Tan(longitude.ToRadians()))
                .ToDegrees()
                .NormalizeDegrees();
        }

        double CalculateSinDeclination(double longitude)
        {
            const double AxialTiltProjectionFactor = 0.39782d;
            return AxialTiltProjectionFactor * Math.Sin(longitude.ToRadians());
        }

        double CalculateLocalHourAngle(double cosineHourAngle)
        {
            double hourAngleDegrees = Math.Acos(cosineHourAngle).ToDegrees();
            return isSunrise ? 360d - hourAngleDegrees : hourAngleDegrees;
        }

        double CalculateLocalMeanTime(double hourAngle, double ascension, double eventApproximateTime)
        {
            const double SolarTransitDriftDegreesPerDay = 0.06571d;
            const double LocalMeanTimeOffsetHours = 6.622d;

            return (
                (hourAngle / DegreesPerHour)
                + ascension
                - (SolarTransitDriftDegreesPerDay * eventApproximateTime)
                - LocalMeanTimeOffsetHours).NormalizeHours();
        }
    }

    private static Error ValidateCoordinates(GeoCoordinates coordinates)
    {
        return !double.IsFinite(coordinates.Latitude)
            ? new Error(
                LatitudeNonFiniteCode,
                LatitudeNonFiniteDescription)
            : !double.IsFinite(coordinates.Longitude)
            ? new Error(
                LongitudeNonFiniteCode,
                LongitudeNonFiniteDescription)
            : coordinates.Latitude is < -90d or > 90d
            ? new Error(
                LatitudeOutOfRangeCode,
                LatitudeOutOfRangeDescription)
            : coordinates.Longitude is < -180d or > 180d
            ? new Error(
                LongitudeOutOfRangeCode,
                LongitudeOutOfRangeDescription)
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
        DateTime utcMidnight = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        DateTimeOffset candidateUtc = new(utcMidnight, TimeSpan.Zero);
        candidateUtc = candidateUtc.Add(TimeSpan.FromHours(utcHours.NormalizeHours()));

        DateTimeOffset localDateTime = TimeZoneInfo.ConvertTime(candidateUtc, timeZone);
        DateOnly convertedLocalDate = DateOnly.FromDateTime(localDateTime.DateTime);

        // Time-zone offsets can only move a UTC timestamp into the neighboring local
        // date, so a single day correction is sufficient without an open-ended loop.
        if (convertedLocalDate < date)
        {
            localDateTime = TimeZoneInfo.ConvertTime(candidateUtc.AddDays(1), timeZone);
        }
        else if (convertedLocalDate > date)
        {
            localDateTime = TimeZoneInfo.ConvertTime(candidateUtc.AddDays(-1), timeZone);
        }

        return DateOnly.FromDateTime(localDateTime.DateTime) != date
            ? throw new UnexpectedStateException(LocalTimestampInvariantDescription)
            : DateTime.SpecifyKind(localDateTime.DateTime, DateTimeKind.Unspecified);
    }

    private static double AdjustQuadrant(double rightAscension, double trueLongitude)
    {
        double longitudeQuadrant = Math.Floor(trueLongitude / 90d) * 90d;
        double rightAscensionQuadrant = Math.Floor(rightAscension / 90d) * 90d;

        return rightAscension + (longitudeQuadrant - rightAscensionQuadrant);
    }

    private readonly record struct SolarEventResult(
        double? UtcHours,
        SolarDaylightCondition DaylightCondition,
        Error Error);
}
