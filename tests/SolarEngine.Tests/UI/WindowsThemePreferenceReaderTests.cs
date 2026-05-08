// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.UI;
using Xunit;

namespace SolarEngine.Tests.UI;

/// <summary>
/// Verifies Windows theme registry values are normalized before native UI theming.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class WindowsThemePreferenceReaderTests
{
    /// <summary>
    /// Verifies missing app-theme values fall back to system dark mode.
    /// </summary>
    [Fact]
    public void ShouldUseDarkWindowFrameFallsBackToSystemDarkModeWhenAppsThemeIsMissing()
    {
        bool shouldUseDarkFrame = WindowsThemePreferenceReader.ShouldUseDarkWindowFrame(null, 0);

        Assert.True(shouldUseDarkFrame);
    }

    /// <summary>
    /// Verifies explicit app-theme values remain authoritative over system values.
    /// </summary>
    [Fact]
    public void ShouldUseDarkWindowFrameUsesAppsThemeWhenBothThemeValuesExist()
    {
        bool shouldUseDarkFrame = WindowsThemePreferenceReader.ShouldUseDarkWindowFrame(1, 0);

        Assert.False(shouldUseDarkFrame);
    }
}
