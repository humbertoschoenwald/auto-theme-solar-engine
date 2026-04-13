using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Locations;

internal interface ISystemLocationProvider
{
    public ValueTask<Result<GeoCoordinates>> GetLocationAsync(CancellationToken cancellationToken = default);
}
