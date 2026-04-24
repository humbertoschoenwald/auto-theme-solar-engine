// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
