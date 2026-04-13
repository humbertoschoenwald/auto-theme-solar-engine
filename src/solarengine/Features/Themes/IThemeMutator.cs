using SolarEngine.Features.Themes.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Themes;

internal interface IThemeMutator
{
    public Result<ThemeMode> Apply(ThemeMode mode);

    public ThemeMode? TryGetCurrentMode();
}
