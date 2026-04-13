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
