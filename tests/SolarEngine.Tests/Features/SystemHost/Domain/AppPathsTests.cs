// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.SystemHost.Domain;
using Xunit;

namespace SolarEngine.Tests.Features.SystemHost.Domain;

/// <summary>
/// Verifies runtime file paths stay rooted in the documented application directory.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class AppPathsTests
{
    /// <summary>
    /// Verifies the explicit constructor normalizes the directory and derived files.
    /// </summary>
    [Fact]
    public void ConstructorNormalizesDirectoryAndDerivedFilePaths()
    {
        string root = Path.Combine(Path.GetTempPath(), "SolarEngine.Tests", Path.GetRandomFileName());
        string nestedPath = Path.Combine(root, ".", "config", "..", "runtime");
        AppPaths appPaths = new(nestedPath);

        Assert.Equal(Path.GetFullPath(nestedPath), appPaths.DirectoryPath);
        Assert.Equal(Path.Combine(appPaths.DirectoryPath, "config.json"), appPaths.ConfigPath);
        Assert.Equal(Path.Combine(appPaths.DirectoryPath, "AutoThemeSolarEngine.log"), appPaths.LogPath);
    }

    /// <summary>
    /// Verifies the default constructor targets the documented LocalAppData directory.
    /// </summary>
    [Fact]
    public void ConstructorUsesLocalAppDataConventionWhenNoDirectoryIsSupplied()
    {
        AppPaths appPaths = new();

        string expectedRoot = Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create),
            "AutoThemeSolarEngine"));

        Assert.Equal(expectedRoot, appPaths.DirectoryPath);
        Assert.EndsWith(Path.Combine("AutoThemeSolarEngine", "config.json"), appPaths.ConfigPath, StringComparison.Ordinal);
        Assert.EndsWith(Path.Combine("AutoThemeSolarEngine", "AutoThemeSolarEngine.log"), appPaths.LogPath, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies blank paths are rejected before runtime files are derived.
    /// </summary>
    [Fact]
    public void ConstructorRejectsBlankDirectoryPath()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new AppPaths(" "));

        Assert.Equal("directoryPath", exception.ParamName);
    }
}
