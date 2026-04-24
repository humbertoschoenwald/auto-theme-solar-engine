// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Features.Updates.Infrastructure;
using SolarEngine.Infrastructure.Logging;
using Xunit;

namespace SolarEngine.Tests.Features.Updates;

/// <summary>
/// Verifies deferred update orchestration writes a complete request before launching helper processes.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class UpdateCoordinatorTests : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "SolarEngine.Tests",
        Path.GetRandomFileName());

    /// <summary>
    /// Creates an isolated application data root for update orchestration tests.
    /// </summary>
    public UpdateCoordinatorTests()
    {
        _ = Directory.CreateDirectory(_directoryPath);
    }

    /// <summary>
    /// Verifies the update request exists before the launcher and helper processes are started.
    /// </summary>
    [Fact]
    public async Task PrepareAndLaunchUpdateAsyncWritesRequestBeforeLaunchingProcesses()
    {
        AppPaths appPaths = new(_directoryPath);
        InstallationMetadataRepository repository = new(appPaths);
        StructuredLogPublisher logPublisher = new(Path.Combine(_directoryPath, "test.log"));
        string downloadedExecutablePath = Path.Combine(
            _directoryPath,
            "auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe");
        List<bool> requestExistsChecks = [];
        List<ProcessStartInfo> startInfos = [];

        async ValueTask<string> DownloadReleaseAssetAsync(
            InstallationMetadata installationMetadata,
            string assetName,
            string assetUrl,
            CancellationToken cancellationToken)
        {
            _ = installationMetadata;
            _ = assetName;
            _ = assetUrl;
            await File.WriteAllTextAsync(downloadedExecutablePath, "stub", cancellationToken).ConfigureAwait(false);
            return downloadedExecutablePath;
        }

        using UpdateCoordinator coordinator = new(
            appPaths,
            logPublisher,
            repository,
            TimeProvider.System,
            static (_, _, _) => ValueTask.FromResult<(CalVersion Version, string Tag, string AssetName, string AssetUrl)?>((
                new CalVersion(26, 4, 5),
                "v26.04.05",
                "auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe",
                "https://example.invalid/self-260405.exe")),
            DownloadReleaseAssetAsync,
            startInfo =>
            {
                requestExistsChecks.Add(File.Exists(repository.UpdateRequestPath));
                startInfos.Add(startInfo);
                return null;
            });

        bool prepared = await coordinator.PrepareAndLaunchUpdateAsync(new AppConfig
        {
            StartWithWindows = true
        });

        string requestJson = File.ReadAllText(repository.UpdateRequestPath);
        string shellPath = InstallationMetadataRepository.ResolveShellExecutablePath();

        Assert.True(prepared);
        Assert.Equal(2, startInfos.Count);
        Assert.All(requestExistsChecks, Assert.True);
        Assert.All(startInfos, startInfo => Assert.Equal(shellPath, startInfo.FileName));
        Assert.Contains(repository.LauncherScriptPath, startInfos[0].Arguments, StringComparison.Ordinal);
        Assert.Contains(repository.HelperScriptPath, startInfos[1].Arguments, StringComparison.Ordinal);
        Assert.Contains("\"LaunchAfterApply\": true", requestJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// Removes the isolated application data root after each test.
    /// </summary>
    public void Dispose()
    {
        if (Directory.Exists(_directoryPath))
        {
            Directory.Delete(_directoryPath, recursive: true);
        }
    }
}
