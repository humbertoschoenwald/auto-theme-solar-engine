// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace SolarEngine.Features.SolarCalculations;

internal static class DependencyInjection
{
    public static IServiceCollection AddSolarCalculationsFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
