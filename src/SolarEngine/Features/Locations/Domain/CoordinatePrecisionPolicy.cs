// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;

namespace SolarEngine.Features.Locations.Domain;

internal static class CoordinatePrecisionPolicy
{
    private const double ComparisonTolerance = 1e-12;
    private const int DefaultDecimalThreshold = 0;
    private const double RoundedZero = 0d;

    public const int DefaultStoredDecimals = 3;
    public const int MinStoredDecimals = 2;
    public const int MaxStoredDecimals = 5;

    public static int NormalizeDecimals(int decimals)
    {
        return decimals <= DefaultDecimalThreshold
            ? DefaultStoredDecimals
            : Math.Clamp(decimals, MinStoredDecimals, MaxStoredDecimals);
    }

    public static GeoCoordinates Reduce(GeoCoordinates coordinates, int decimals)
    {
        ArgumentNullException.ThrowIfNull(coordinates);

        int normalizedDecimals = NormalizeDecimals(decimals);
        return new GeoCoordinates
        {
            Latitude = Reduce(coordinates.Latitude, normalizedDecimals),
            Longitude = Reduce(coordinates.Longitude, normalizedDecimals)
        };
    }

    public static double Reduce(double value, int decimals)
    {
        double rounded = Math.Round(value, NormalizeDecimals(decimals), MidpointRounding.AwayFromZero);
        return Math.Abs(rounded) < ComparisonTolerance ? RoundedZero : rounded;
    }

    public static string Format(double value, int decimals)
    {
        int normalizedDecimals = NormalizeDecimals(decimals);
        return Reduce(value, normalizedDecimals).ToString($"F{normalizedDecimals}", CultureInfo.InvariantCulture);
    }

    public static bool AreEquivalent(double left, double right)
    {
        return Math.Abs(left - right) < ComparisonTolerance;
    }
}
