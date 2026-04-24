// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.SystemHost.Infrastructure;
using SolarEngine.Infrastructure.Localization;
using SolarEngine.Infrastructure.Logging;

namespace SolarEngine.Features.SystemHost;

internal static class DependencyInjection
{
    public static IServiceCollection AddSystemHostFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<AppPaths>();

        _ = services.AddSingleton(provider =>
        {
            AppPaths appPaths = provider.GetRequiredService<AppPaths>();
            return new StructuredLogPublisher(appPaths.LogPath);
        });

        _ = services.AddSingleton<AppLocalization>();
        _ = services.AddSingleton<ConfigurationRepository>();
        _ = services.AddSingleton<WindowsStartupRegistrar>();
        _ = services.AddSingleton<ApplicationLifecycleOrchestrator>();

        return services;
    }
}
