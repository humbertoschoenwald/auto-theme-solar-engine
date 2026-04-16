using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Locations.Domain;

internal sealed record GeoCoordinates
{
    public required double Latitude { get; init; }

    public required double Longitude { get; init; }

    public static Result<GeoCoordinates> Create(double latitude, double longitude)
    {
        return !double.IsFinite(latitude) || !double.IsFinite(longitude)
            ? Result<GeoCoordinates>.Failure(
                new Error(
                    "locations.coordinates.nonfinite",
                    "Reject non-finite coordinates before persisting domain state."))
            : latitude is < -90d or > 90d
            ? Result<GeoCoordinates>.Failure(
                new Error(
                    "locations.coordinates.latitude_range",
                    "Constrain latitude to the valid geospatial domain."))
            : longitude is < -180d or > 180d
            ? Result<GeoCoordinates>.Failure(
                new Error(
                    "locations.coordinates.longitude_range",
                    "Constrain longitude to the valid geospatial domain."))
            : Result<GeoCoordinates>.Success(new GeoCoordinates
            {
                Latitude = latitude,
                Longitude = longitude
            });
    }
}
