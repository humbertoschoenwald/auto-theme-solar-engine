namespace SolarEngine.Infrastructure.Serialization;

internal sealed record PersistedAppConfig
{
    public string? ProtectedCoordinates { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public bool UseWindowsLocation { get; init; } = true;

    public int LocationPrecisionDecimals { get; init; } = 3;

    public bool StartWithWindows { get; init; } = true;

    public bool StartMinimized { get; init; } = true;

    public bool UseHighPriority { get; init; } = true;

    public bool AddExtraMinuteAtSunset { get; init; } = true;

    public bool AutomaticUpdatesEnabled { get; init; } = true;

    public string LanguageCode { get; init; } = "en";

    public int CheckIntervalSeconds { get; init; } = 30;

    public bool IsConfigured { get; init; }
}
