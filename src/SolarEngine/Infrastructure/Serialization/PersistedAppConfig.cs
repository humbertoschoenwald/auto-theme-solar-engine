namespace SolarEngine.Infrastructure.Serialization;

internal sealed record PersistedAppConfig
{
    public string? ProtectedCoordinates { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public bool UseWindowsLocation { get; init; }

    public int LocationPrecisionDecimals { get; init; } = 3;

    public bool StartWithWindows { get; init; } = true;

    public bool StartMinimized { get; init; } = true;

    public bool UseHighPriority { get; init; }

    public int CheckIntervalSeconds { get; init; } = 30;

    public bool IsConfigured { get; init; }
}
