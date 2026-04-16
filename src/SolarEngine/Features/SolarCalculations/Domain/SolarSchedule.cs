namespace SolarEngine.Features.SolarCalculations.Domain;

internal sealed record SolarSchedule(
    DateOnly Date,
    DateTime? SunriseLocal,
    DateTime? SunsetLocal,
    SolarDaylightCondition DaylightCondition);
