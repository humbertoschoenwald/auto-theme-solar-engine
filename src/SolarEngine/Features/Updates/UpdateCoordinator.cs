using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Features.Updates.Infrastructure;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Updates;

internal sealed class UpdateCoordinator(
    AppPaths appPaths,
    StructuredLogPublisher logPublisher,
    InstallationMetadataRepository installationMetadataRepository,
    GitHubReleaseFeedClient gitHubReleaseFeedClient,
    TimeProvider timeProvider) : IDisposable
{
    private readonly Lock _snapshotGate = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private UpdateStatusSnapshot? _snapshot;

    public UpdateStatusSnapshot GetSnapshot()
    {
        lock (_snapshotGate)
        {
            return _snapshot ?? BuildEmptySnapshot();
        }
    }

    public void EnsureInstallationReady()
    {
        try
        {
            installationMetadataRepository.EnsureCurrentInstallationRegistered();
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or FileNotFoundException
            or Win32Exception
            or UnexpectedStateException)
        {
            logPublisher.Write($"Installation bootstrap skipped: {exception.Message}");
        }
    }

    public async ValueTask<UpdateStatusSnapshot> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            InstallationMetadata installationMetadata = installationMetadataRepository.Load();
            CalVersion currentVersion = ResolveCurrentVersion();
            (CalVersion Version, string Tag, string AssetName, string AssetUrl)? latestRelease =
                await gitHubReleaseFeedClient
                    .FindLatestMatchingReleaseAsync(installationMetadata.ReleaseFlavor, currentVersion, cancellationToken)
                    .ConfigureAwait(false);

            UpdateStatusSnapshot snapshot = BuildSnapshot(
                installationMetadata,
                currentVersion,
                latestRelease?.Tag ?? currentVersion.ToTag(),
                latestRelease?.AssetName,
                latestRelease is not null);

            lock (_snapshotGate)
            {
                _snapshot = snapshot;
            }

            return snapshot;
        }
        finally
        {
            _ = _operationGate.Release();
        }
    }

    public async ValueTask<bool> PrepareAndLaunchUpdateAsync(
        AppConfig configuration,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            InstallationMetadata installationMetadata = installationMetadataRepository.Load();
            CalVersion currentVersion = ResolveCurrentVersion();
            (CalVersion Version, string Tag, string AssetName, string AssetUrl)? latestRelease =
                await gitHubReleaseFeedClient
                    .FindLatestMatchingReleaseAsync(installationMetadata.ReleaseFlavor, currentVersion, cancellationToken)
                    .ConfigureAwait(false);

            if (latestRelease is null)
            {
                lock (_snapshotGate)
                {
                    _snapshot = BuildSnapshot(
                        installationMetadata,
                        currentVersion,
                        currentVersion.ToTag(),
                        pendingAssetName: null,
                        isUpdateAvailable: false);
                }

                return false;
            }

            string stagedExecutablePath = await DownloadReleaseAssetAsync(
                installationMetadata,
                latestRelease.Value.AssetName,
                latestRelease.Value.AssetUrl,
                cancellationToken).ConfigureAwait(false);

            installationMetadataRepository.EnsureHelperScript();
            bool launchAfterApply = !TryLaunchRestartLauncher(installationMetadata.InstalledExecutablePath);
            installationMetadataRepository.SaveUpdateRequest(new PersistedUpdateRequest
            {
                ProcessId = Environment.ProcessId,
                DownloadedExecutablePath = stagedExecutablePath,
                InstalledExecutablePath = installationMetadata.InstalledExecutablePath,
                StartWithWindows = configuration.StartWithWindows,
                LaunchAfterApply = launchAfterApply
            });

            LaunchHelper(installationMetadata);
            logPublisher.Write($"Update prepared for {latestRelease.Value.Tag} using asset {latestRelease.Value.AssetName}.");

            lock (_snapshotGate)
            {
                _snapshot = BuildSnapshot(
                    installationMetadata,
                    currentVersion,
                    latestRelease.Value.Tag,
                    latestRelease.Value.AssetName,
                    isUpdateAvailable: true);
            }

            return true;
        }
        finally
        {
            _ = _operationGate.Release();
        }
    }

    public void RecordCheckFailure(string errorMessage)
    {
        InstallationMetadata installationMetadata = installationMetadataRepository.Load();
        CalVersion currentVersion = ResolveCurrentVersion();
        UpdateStatusSnapshot currentSnapshot = GetSnapshot();

        lock (_snapshotGate)
        {
            _snapshot = BuildSnapshot(
                installationMetadata,
                currentVersion,
                currentSnapshot.LatestVersionTag ?? currentVersion.ToTag(),
                currentSnapshot.PendingAssetName,
                isUpdateAvailable: false,
                lastCheckErrorMessage: errorMessage);
        }
    }

    private async ValueTask<string> DownloadReleaseAssetAsync(
        InstallationMetadata installationMetadata,
        string assetName,
        string assetUrl,
        CancellationToken cancellationToken)
    {
        string preferredPath = Path.Combine(installationMetadata.InstallDirectory, assetName);

        try
        {
            _ = Directory.CreateDirectory(installationMetadata.InstallDirectory);
            await DownloadToPathAsync(assetUrl, preferredPath, cancellationToken).ConfigureAwait(false);
            return preferredPath;
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException)
        {
            logPublisher.Write(
                $"Direct install-directory staging failed and is falling back to LocalAppData: {exception.Message}");
        }

        string fallbackDirectory = Path.Combine(appPaths.DirectoryPath, "updates");
        _ = Directory.CreateDirectory(fallbackDirectory);
        string fallbackPath = Path.Combine(fallbackDirectory, assetName);
        await DownloadToPathAsync(assetUrl, fallbackPath, cancellationToken).ConfigureAwait(false);
        return fallbackPath;
    }

    private static async ValueTask DownloadToPathAsync(
        string assetUrl,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        using HttpClient client = new();
        await using Stream downloadStream = await client.GetStreamAsync(assetUrl, cancellationToken).ConfigureAwait(false);
        await using FileStream fileStream = new(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await downloadStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
        await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private void LaunchHelper(InstallationMetadata installationMetadata)
    {
        if (installationMetadata.InstallationMode == InstallationMode.ProgramFiles
            && !string.IsNullOrWhiteSpace(installationMetadata.ElevatedTaskName))
        {
            _ = Process.Start(new ProcessStartInfo
            {
                FileName = "schtasks.exe",
                Arguments = $"/Run /TN \"{installationMetadata.ElevatedTaskName}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            return;
        }

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = "pwsh",
            Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{installationMetadataRepository.HelperScriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        });
    }

    private bool TryLaunchRestartLauncher(string installedExecutablePath)
    {
        try
        {
            Process? launcherProcess = Process.Start(new ProcessStartInfo
            {
                FileName = "pwsh",
                Arguments =
                    $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{installationMetadataRepository.LauncherScriptPath}\" -RequestPath \"{installationMetadataRepository.UpdateRequestPath}\" -InstalledPath \"{installedExecutablePath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            return launcherProcess is not null;
        }
        catch (Exception exception) when (
            exception is Win32Exception
            or FileNotFoundException)
        {
            logPublisher.Write($"Restart launcher could not be started and helper relaunch will be used instead: {exception.Message}");
            return false;
        }
    }

    private static CalVersion ResolveCurrentVersion()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? new CalVersion(0, 0, 0) : new CalVersion(version.Major, version.Minor, version.Build);
    }

    private UpdateStatusSnapshot BuildEmptySnapshot()
    {
        InstallationMetadata installationMetadata = installationMetadataRepository.Load();
        return new UpdateStatusSnapshot
        {
            CurrentVersion = ResolveCurrentVersion(),
            ReleaseFlavor = installationMetadata.ReleaseFlavor,
            InstallationMode = installationMetadata.InstallationMode,
            LatestVersionTag = null,
            PendingAssetName = null,
            IsUpdateAvailable = false,
            LastCheckedAtUtc = null,
            LastCheckErrorMessage = null
        };
    }

    public void Dispose()
    {
        _operationGate.Dispose();
    }

    private UpdateStatusSnapshot BuildSnapshot(
        InstallationMetadata installationMetadata,
        CalVersion currentVersion,
        string? latestVersionTag,
        string? pendingAssetName,
        bool isUpdateAvailable,
        string? lastCheckErrorMessage = null)
    {
        return new UpdateStatusSnapshot
        {
            CurrentVersion = currentVersion,
            ReleaseFlavor = installationMetadata.ReleaseFlavor,
            InstallationMode = installationMetadata.InstallationMode,
            LatestVersionTag = latestVersionTag,
            PendingAssetName = pendingAssetName,
            IsUpdateAvailable = isUpdateAvailable,
            LastCheckedAtUtc = timeProvider.GetUtcNow(),
            LastCheckErrorMessage = lastCheckErrorMessage
        };
    }
}
