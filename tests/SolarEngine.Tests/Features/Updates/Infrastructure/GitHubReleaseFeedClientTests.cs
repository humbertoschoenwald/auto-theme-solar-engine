// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Features.Updates.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Updates.Infrastructure;

/// <summary>
/// Verifies GitHub release parsing keeps update delivery pinned to the supported self-contained asset.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class GitHubReleaseFeedClientTests
{
    /// <summary>
    /// Verifies the selector prefers the newest self-contained asset above the current version.
    /// </summary>
    [Fact]
    public void SelectLatestMatchingReleasePicksNewestSelfContainedAssetAboveCurrentVersion()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            [
              {
                "draft": false,
                "tag_name": "v26.04.02",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe",
                    "browser_download_url": "https://example.invalid/self-260402.exe"
                  },
                  {
                    "name": "auto-theme-solar-engine-win-x64-framework-dependent-v26.04.02.exe",
                    "browser_download_url": "https://example.invalid/fd-260402.exe"
                  }
                ]
              },
              {
                "draft": false,
                "tag_name": "v26.04.03",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe",
                    "browser_download_url": "https://example.invalid/self-260403.exe"
                  }
                ]
              }
            ]
            """);

        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? match =
            GitHubReleaseFeedClient.SelectLatestMatchingRelease(
                document.RootElement,
                ReleaseFlavor.SelfContained,
                new CalVersion(26, 4, 1));

        _ = Assert.NotNull(match);
        Assert.Equal("v26.04.03", match.Value.Tag);
        Assert.Equal("auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe", match.Value.AssetName);
        Assert.Equal("https://example.invalid/self-260403.exe", match.Value.AssetUrl);
    }

    /// <summary>
    /// Verifies legacy framework-dependent metadata still resolves the newest supported self-contained asset.
    /// </summary>
    [Fact]
    public void SelectLatestMatchingReleaseTreatsLegacyFrameworkDependentFlavorAsSelfContained()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            [
              {
                "draft": true,
                "tag_name": "v26.04.05",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-framework-dependent-v26.04.05.exe",
                    "browser_download_url": "https://example.invalid/fd-260405.exe"
                  }
                ]
              },
              {
                "draft": false,
                "tag_name": "v26.04.01",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-framework-dependent-v26.04.01.exe",
                    "browser_download_url": "https://example.invalid/fd-260401.exe"
                  }
                ]
              },
              {
                "draft": false,
                "tag_name": "v26.04.04",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
                    "browser_download_url": "https://example.invalid/self-260404.exe"
                  }
                ]
              }
            ]
            """);

        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? match =
            GitHubReleaseFeedClient.SelectLatestMatchingRelease(
                document.RootElement,
                ReleaseFlavor.FrameworkDependent,
                new CalVersion(26, 4, 3));

        _ = Assert.NotNull(match);
        Assert.Equal("v26.04.04", match.Value.Tag);
        Assert.Equal("auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe", match.Value.AssetName);
    }

    /// <summary>
    /// Verifies the selector reports no candidate when a release exposes no supported self-contained asset.
    /// </summary>
    [Fact]
    public void SelectLatestMatchingReleaseReturnsNullWhenNoSupportedAssetExists()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            [
              {
                "draft": false,
                "tag_name": "v26.04.04",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-framework-dependent-v26.04.04.exe",
                    "browser_download_url": "https://example.invalid/fd-260404.exe"
                  }
                ]
              }
            ]
            """);

        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? match =
            GitHubReleaseFeedClient.SelectLatestMatchingRelease(
                document.RootElement,
                ReleaseFlavor.FrameworkDependent,
                new CalVersion(26, 4, 3));

        Assert.Null(match);
    }

    /// <summary>
    /// Verifies releases marked as YANKED in the GitHub release name are excluded.
    /// </summary>
    [Fact]
    public void SelectLatestMatchingReleaseIgnoresYankedReleaseNames()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            [
              {
                "draft": false,
                "name": "v26.04.04 [YANKED]",
                "tag_name": "v26.04.04",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
                    "browser_download_url": "https://example.invalid/self-260404.exe"
                  }
                ]
              },
              {
                "draft": false,
                "name": "v26.04.05",
                "tag_name": "v26.04.05",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe",
                    "browser_download_url": "https://example.invalid/self-260405.exe"
                  }
                ]
              }
            ]
            """);

        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? match =
            GitHubReleaseFeedClient.SelectLatestMatchingRelease(
                document.RootElement,
                ReleaseFlavor.SelfContained,
                new CalVersion(26, 4, 3));

        _ = Assert.NotNull(match);
        Assert.Equal("v26.04.05", match.Value.Tag);
        Assert.Equal("auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe", match.Value.AssetName);
    }

    /// <summary>
    /// Verifies releases marked as YANKED in the GitHub release body are excluded.
    /// </summary>
    [Fact]
    public void SelectLatestMatchingReleaseIgnoresYankedReleaseBodies()
    {
        using JsonDocument document = JsonDocument.Parse(
            """
            [
              {
                "draft": false,
                "tag_name": "v26.04.04",
                "body": "YANKED: updater relaunch is broken.",
                "assets": [
                  {
                    "name": "auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
                    "browser_download_url": "https://example.invalid/self-260404.exe"
                  }
                ]
              }
            ]
            """);

        (CalVersion Version, string Tag, string AssetName, string AssetUrl)? match =
            GitHubReleaseFeedClient.SelectLatestMatchingRelease(
                document.RootElement,
                ReleaseFlavor.FrameworkDependent,
                new CalVersion(26, 4, 3));

        Assert.Null(match);
    }
}
