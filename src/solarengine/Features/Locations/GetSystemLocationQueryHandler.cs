using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Locations;

internal sealed class GetSystemLocationQueryHandler(ISystemLocationProvider systemLocationProvider)
{
    public ValueTask<Result<GeoCoordinates>> HandleAsync(GetSystemLocationQuery _, CancellationToken cancellationToken = default)
    {
        return systemLocationProvider.GetLocationAsync(cancellationToken);
    }
}
