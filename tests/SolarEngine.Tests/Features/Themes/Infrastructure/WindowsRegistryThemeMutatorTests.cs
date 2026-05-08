// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.Themes.Domain;
using SolarEngine.Features.Themes.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Themes.Infrastructure;

/// <summary>
/// Verifies Windows registry theme state is normalized before status rendering.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class WindowsRegistryThemeMutatorTests
{
    /// <summary>
    /// Verifies a missing app-theme value does not make an otherwise dark shell look unknown.
    /// </summary>
    [Fact]
    public void ResolveThemeModeFallsBackToSystemThemeWhenAppsThemeIsMissing()
    {
        ThemeMode? mode = WindowsRegistryThemeMutator.ResolveThemeMode(null, 0);

        Assert.Equal(ThemeMode.Dark, mode);
    }

    /// <summary>
    /// Verifies a missing system-theme value can still report the app-theme value.
    /// </summary>
    [Fact]
    public void ResolveThemeModeFallsBackToAppsThemeWhenSystemThemeIsMissing()
    {
        ThemeMode? mode = WindowsRegistryThemeMutator.ResolveThemeMode(1, null);

        Assert.Equal(ThemeMode.Light, mode);
    }

    /// <summary>
    /// Verifies explicit mixed app and system values remain unknown.
    /// </summary>
    [Fact]
    public void ResolveThemeModeKeepsExplicitMixedThemeValuesUnknown()
    {
        ThemeMode? mode = WindowsRegistryThemeMutator.ResolveThemeMode(1, 0);

        Assert.Null(mode);
    }
}
