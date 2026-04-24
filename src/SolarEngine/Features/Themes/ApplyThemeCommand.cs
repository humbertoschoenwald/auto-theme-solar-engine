// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Themes.Domain;

namespace SolarEngine.Features.Themes;

internal readonly record struct ApplyThemeCommand(ThemeMode Mode);
