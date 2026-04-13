using SolarEngine.Features.Themes.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Themes;

internal sealed class ApplyThemeCommandHandler(IThemeMutator themeMutator)
{
    private readonly IThemeMutator _themeMutator = themeMutator ?? throw new ArgumentNullException(nameof(themeMutator));

    public Result<ThemeMode> Handle(ApplyThemeCommand command)
    {
        return _themeMutator.Apply(command.Mode);
    }
}
