// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SolarEngine.Features.Locations.Infrastructure;
namespace SolarEngine.Features.Locations;

internal static class DependencyInjection
{
    public static IServiceCollection AddLocationsFeature(this IServiceCollection services)
    {
        _ = services.AddSingleton<ISystemLocationProvider, WindowsLocationProvider>();
        _ = services.AddSingleton<GetSystemLocationQueryHandler>();
        return services;
    }
}
