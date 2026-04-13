using Microsoft.Extensions.DependencyInjection;
using SolarEngine.Features.Themes.Infrastructure;

namespace SolarEngine.Features.Themes;

internal static class DependencyInjection
{
    public static IServiceCollection AddThemesFeature(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<WindowsRegistryThemeMutator>();
        _ = services.AddSingleton<IThemeMutator>(static serviceProvider =>
            serviceProvider.GetRequiredService<WindowsRegistryThemeMutator>());

        _ = services.AddSingleton<ApplyThemeCommandHandler>();
        _ = services.AddSingleton<ThemeTransitionOrchestrator>();

        return services;
    }
}
