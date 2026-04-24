// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Locations.Domain;
using SolarEngine.Infrastructure.Localization;

namespace SolarEngine.Features.SystemHost.Domain;

internal sealed record AppConfig
{
    private const int DefaultCheckIntervalSeconds = 30;
    private const int MaximumCheckIntervalSeconds = 300;
    private const int MinimumCheckIntervalSeconds = 10;
    private const string NonFiniteLatitudeDescription = "Use a finite latitude value to preserve deterministic solar calculations.";
    private const string NonFiniteLongitudeDescription = "Use a finite longitude value to preserve deterministic solar calculations.";

    public double Latitude
    {
        get;
        init => field = double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), NonFiniteLatitudeDescription);
    }

    public double Longitude
    {
        get;
        init => field = double.IsFinite(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), NonFiniteLongitudeDescription);
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
            < MinimumCheckIntervalSeconds => MinimumCheckIntervalSeconds,
            > MaximumCheckIntervalSeconds => MaximumCheckIntervalSeconds,
            _ => value
        };
    } = DefaultCheckIntervalSeconds;

    public bool IsConfigured
    {
        get; init;
    }
}
