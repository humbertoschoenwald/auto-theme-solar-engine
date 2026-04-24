// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Xml.Linq;
using Xunit;

namespace SolarEngine.Tests.Features.Updates;

/// <summary>
/// Verifies release version metadata flows through the effective project version instead of a stale literal.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class ProjectVersionMetadataTests
{
    /// <summary>
    /// Verifies assembly metadata version fields stay bound to the effective project version.
    /// </summary>
    [Fact]
    public void SolarEngineProjectBindsAssemblyMetadataToVersionProperty()
    {
        string projectPath = Path.Combine(
            ResolveRepositoryRoot(),
            "src",
            "SolarEngine",
            "SolarEngine.csproj");
        XDocument document = XDocument.Load(projectPath);
        XElement propertyGroup = document.Root?.Element("PropertyGroup")
            ?? throw new Xunit.Sdk.XunitException("Load the application property group before asserting version metadata.");

        string assemblyVersion = propertyGroup.Element("AssemblyVersion")?.Value
            ?? throw new Xunit.Sdk.XunitException("Resolve the AssemblyVersion element before asserting version metadata.");
        string fileVersion = propertyGroup.Element("FileVersion")?.Value
            ?? throw new Xunit.Sdk.XunitException("Resolve the FileVersion element before asserting version metadata.");

        Assert.Equal("$(Version)", assemblyVersion);
        Assert.Equal("$(Version)", fileVersion);
    }

    private static string ResolveRepositoryRoot()
    {
        string? directoryPath = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directoryPath))
        {
            if (File.Exists(Path.Combine(directoryPath, "SolarEngine.slnx")))
            {
                return directoryPath;
            }

            directoryPath = Directory.GetParent(directoryPath)?.FullName;
        }

        throw new DirectoryNotFoundException("Resolve the repository root before asserting project version metadata.");
    }
}
