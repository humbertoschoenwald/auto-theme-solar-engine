// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Features.Themes.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Themes.Infrastructure;

/// <summary>
/// Verifies generated theme metadata follows Windows light and dark mode state.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class ThemeMetadataSynchronizerTests
{
    /// <summary>
    /// Verifies synchronization preserves the user's current theme assets.
    /// </summary>
    [Fact]
    public void SynchronizePreservesThemeAssetsAndUpdatesModeMetadata()
    {
        string tempDirectory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string currentThemePath = Path.Combine(tempDirectory, "Custom.theme");
            File.WriteAllText(
                currentThemePath,
                string.Join(
                    Environment.NewLine,
                    [
                        "[Theme]",
                        "DisplayName=Unsaved Theme",
                        "ThemeId={95B1EDB1-BE50-48A3-89F4-B33AD5B890BA}",
                        string.Empty,
                        "[Control Panel\\Desktop]",
                        "Wallpaper=%USERPROFILE%\\Pictures\\wallpaper.png",
                        "PicturePosition=4",
                        string.Empty,
                        "[VisualStyles]",
                        "Path=%SystemRoot%\\resources\\Themes\\Aero\\Aero.msstyles",
                        "AutoColorization=1",
                        "ColorizationColor=0X624213",
                        "SystemMode=Dark",
                        "AppMode=Dark"
                    ]),
                Encoding.Latin1);

            string generatedDirectory = Path.Combine(tempDirectory, "Generated");

            string generatedThemePath = ThemeMetadataSynchronizer.Synchronize(
                ThemeMode.Light,
                currentThemePath,
                generatedDirectory);

            Assert.Equal(Path.GetFullPath(Path.Combine(generatedDirectory, "Light.theme")), generatedThemePath);

            string generatedTheme = File.ReadAllText(generatedThemePath, Encoding.Latin1);
            Assert.Contains("DisplayName=Light", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("SystemMode=Light", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("AppMode=Light", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("Wallpaper=%USERPROFILE%\\Pictures\\wallpaper.png", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("ColorizationColor=0X624213", generatedTheme, StringComparison.Ordinal);
            Assert.DoesNotContain("SystemMode=Dark", generatedTheme, StringComparison.Ordinal);
            Assert.DoesNotContain("AppMode=Dark", generatedTheme, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Verifies missing sections are added instead of dropping back to a generic theme.
    /// </summary>
    [Fact]
    public void SynchronizeAddsMissingModeMetadataSections()
    {
        string tempDirectory = Directory.CreateTempSubdirectory().FullName;
        try
        {
            string currentThemePath = Path.Combine(tempDirectory, "Custom.theme");
            File.WriteAllText(
                currentThemePath,
                string.Join(
                    Environment.NewLine,
                    [
                        "[Theme]",
                        "DisplayName=Unsaved Theme",
                        string.Empty,
                        "[Control Panel\\Desktop]",
                        "Wallpaper=%USERPROFILE%\\Pictures\\wallpaper.png"
                    ]),
                Encoding.Latin1);

            string generatedDirectory = Path.Combine(tempDirectory, "Generated");

            string generatedThemePath = ThemeMetadataSynchronizer.Synchronize(
                ThemeMode.Dark,
                currentThemePath,
                generatedDirectory);

            string generatedTheme = File.ReadAllText(generatedThemePath, Encoding.Latin1);
            Assert.Equal(Path.GetFullPath(Path.Combine(generatedDirectory, "Dark.theme")), generatedThemePath);
            Assert.Contains("[VisualStyles]", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("DisplayName=Dark", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("SystemMode=Dark", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("AppMode=Dark", generatedTheme, StringComparison.Ordinal);
            Assert.Contains("Wallpaper=%USERPROFILE%\\Pictures\\wallpaper.png", generatedTheme, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }
}
