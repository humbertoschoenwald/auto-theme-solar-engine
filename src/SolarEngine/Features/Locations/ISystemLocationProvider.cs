// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Locations.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Locations;

internal interface ISystemLocationProvider
{
    public ValueTask<SystemLocationAccessState> GetAccessStateAsync(CancellationToken cancellationToken = default);

    public ValueTask<Result<GeoCoordinates>> GetLocationAsync(CancellationToken cancellationToken = default);
}
