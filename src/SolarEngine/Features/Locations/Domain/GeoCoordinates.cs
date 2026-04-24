// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Locations.Domain;

internal sealed record GeoCoordinates
{
    private const string LatitudeRangeCode = "locations.coordinates.latitude_range";
    private const string LatitudeRangeDescription = "Constrain latitude to the valid geospatial domain.";
    private const double LatitudeRangeLimit = 90d;
    private const string LongitudeRangeCode = "locations.coordinates.longitude_range";
    private const string LongitudeRangeDescription = "Constrain longitude to the valid geospatial domain.";
    private const double LongitudeRangeLimit = 180d;
    private const string NonFiniteCode = "locations.coordinates.nonfinite";
    private const string NonFiniteDescription = "Reject non-finite coordinates before persisting domain state.";

    public required double Latitude
    {
        get; init;
    }

    public required double Longitude
    {
        get; init;
    }

    public static Result<GeoCoordinates> Create(double latitude, double longitude)
    {
        return !double.IsFinite(latitude) || !double.IsFinite(longitude)
            ? Result<GeoCoordinates>.Failure(
                new Error(
                    NonFiniteCode,
                    NonFiniteDescription))
            : latitude is < -LatitudeRangeLimit or > LatitudeRangeLimit
            ? Result<GeoCoordinates>.Failure(
                new Error(
                    LatitudeRangeCode,
                    LatitudeRangeDescription))
            : longitude is < -LongitudeRangeLimit or > LongitudeRangeLimit
            ? Result<GeoCoordinates>.Failure(
                new Error(
                    LongitudeRangeCode,
                    LongitudeRangeDescription))
            : Result<GeoCoordinates>.Success(new GeoCoordinates
            {
                Latitude = latitude,
                Longitude = longitude
            });
    }
}
