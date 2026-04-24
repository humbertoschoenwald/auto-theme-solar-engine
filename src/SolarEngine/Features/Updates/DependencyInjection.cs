// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SolarEngine.Features.Updates.Infrastructure;

namespace SolarEngine.Features.Updates;

internal static class DependencyInjection
{
    public static IServiceCollection AddUpdatesFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<InstallationMetadataRepository>();
        _ = services.AddSingleton<UpdateCoordinator>();

        return services;
    }
}
