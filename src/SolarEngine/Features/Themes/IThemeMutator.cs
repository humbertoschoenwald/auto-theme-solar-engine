// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Themes.Domain;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Themes;

internal interface IThemeMutator
{
    public Result<ThemeMode> Apply(ThemeMode mode);

    public ThemeMode? TryGetCurrentMode();
}
