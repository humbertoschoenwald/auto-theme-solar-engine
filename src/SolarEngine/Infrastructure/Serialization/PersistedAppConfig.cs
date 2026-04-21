namespace SolarEngine.Infrastructure.Serialization;

internal sealed record PersistedAppConfig
{
    private const int DefaultLocationPrecisionDecimals = 3;
    private const string DefaultLanguageCode = "en";
    private const int DefaultCheckIntervalSeconds = 30;

    public string? ProtectedCoordinates { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public bool UseWindowsLocation { get; init; } = true;

    public int LocationPrecisionDecimals { get; init; } = DefaultLocationPrecisionDecimals;

    public bool StartWithWindows { get; init; } = true;

    public bool StartMinimized { get; init; } = true;

    public bool UseHighPriority { get; init; } = true;

    public bool AddExtraMinuteAtSunset { get; init; } = true;

    public bool AutomaticUpdatesEnabled { get; init; } = true;

    public string LanguageCode { get; init; } = DefaultLanguageCode;

    public int CheckIntervalSeconds { get; init; } = DefaultCheckIntervalSeconds;

    public bool IsConfigured { get; init; }
}
