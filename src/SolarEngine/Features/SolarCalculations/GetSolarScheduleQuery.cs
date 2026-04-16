using SolarEngine.Features.Locations.Domain;

namespace SolarEngine.Features.SolarCalculations;

internal readonly record struct GetSolarScheduleQuery(
    DateOnly Date,
    GeoCoordinates Coordinates,
    TimeZoneInfo TimeZone);
