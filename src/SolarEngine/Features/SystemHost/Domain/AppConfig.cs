using SolarEngine.Features.Locations.Domain;
using SolarEngine.Infrastructure.Localization;

namespace SolarEngine.Features.SystemHost.Domain;

internal sealed record AppConfig
{
    public double Latitude
    {
        get;
        init => field = double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Use a finite latitude value to preserve deterministic solar calculations.");
    }

    public double Longitude
    {
        get;
        init => field = double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), "Use a finite longitude value to preserve deterministic solar calculations.");
    }

    public bool UseWindowsLocation { get; init; } = true;

    public int LocationPrecisionDecimals
    {
        get;
        init => field = CoordinatePrecisionPolicy.NormalizeDecimals(value);
    } = CoordinatePrecisionPolicy.DefaultStoredDecimals;

    public bool StartWithWindows { get; init; } = true;

    public bool StartMinimized { get; init; } = true;

    public bool UseHighPriority { get; init; } = true;

    public bool AddExtraMinuteAtSunset { get; init; } = true;

    public bool AutomaticUpdatesEnabled { get; init; } = true;

    public string LanguageCode
    {
        get;
        init => field = AppLanguageCodes.Normalize(value);
    } = AppLanguageCodes.Default;

    public int CheckIntervalSeconds
    {
        get;
        init => field = value switch
        {
            < 10 => 10,
            > 300 => 300,
            _ => value
        };
    } = 30;

    public bool IsConfigured { get; init; }
}
