using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Features.Updates.Infrastructure;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Updates;

internal sealed class UpdateCoordinator : IDisposable
{
    private readonly AppPaths _appPaths;
    private readonly StructuredLogPublisher _logPublisher;
    private readonly InstallationMetadataRepository _installationMetadataRepository;
    private readonly Func<ReleaseFlavor, CalVersion, CancellationToken, ValueTask<(CalVersion Version, string Tag, string AssetName, string AssetUrl)?>> _findLatestMatchingReleaseAsync;
    private readonly Func<InstallationMetadata, string, string, CancellationToken, ValueTask<string>> _downloadReleaseAssetAsync;
    private readonly Func<ProcessStartInfo, Process?> _processStarter;
    private readonly TimeProvider _timeProvider;
    private readonly Lock _snapshotGate = new();
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private UpdateStatusSnapshot? _snapshot;

    public UpdateCoordinator(
        AppPaths appPaths,
        StructuredLogPublisher logPublisher,
        InstallationMetadataRepository installationMetadataRepository,
        GitHubReleaseFeedClient gitHubReleaseFeedClient,
        TimeProvider timeProvider)
    {
        _appPaths = appPaths;
        _logPublisher = logPublisher;
        _installationMetadataRepository = installationMetadataRepository;
        _findLatestMatchingReleaseAsync = gitHubReleaseFeedClient.FindLatestMatchingReleaseAsync;
        _downloadReleaseAssetAsync = DownloadReleaseAssetAsync;
        _processStarter = Process.Start;
        _timeProvider = timeProvider;
    }

    internal UpdateCoordinator(
        AppPaths appPaths,
        StructuredLogPublisher logPublisher,
        InstallationMetadataRepository installationMetadataRepository,
        TimeProvider timeProvider,
        Func<ReleaseFlavor, CalVersion, CancellationToken, ValueTask<(CalVersion Version, string Tag, string AssetName, string AssetUrl)?>> findLatestMatchingReleaseAsync,
        Func<InstallationMetadata, string, string, CancellationToken, ValueTask<string>> downloadReleaseAssetAsync,
        Func<ProcessStartInfo, Process?> processStarter)
    {
        _appPaths = appPaths;
        _logPublisher = logPublisher;
        _installationMetadataRepository = installationMetadataRepository;
        _findLatestMatchingReleaseAsync = findLatestMatchingReleaseAsync;
        _downloadReleaseAssetAsync = downloadReleaseAssetAsync;
        _processStarter = processStarter;
        _timeProvider = timeProvider;
    }

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
            _installationMetadataRepository.EnsureCurrentInstallationRegistered();
        }
        catch (Exception exception) when (
            exception is IOException
            or UnauthorizedAccessException
            or FileNotFoundException
            or Win32Exception
            or UnexpectedStateException)
        {
            _logPublisher.Write($"Installation bootstrap skipped: {exception.Message}");
        }
    }

    public async ValueTask<UpdateStatusSnapshot> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            InstallationMetadata installationMetadata = _installationMetadataRepository.Load();
            CalVersion currentVersion = ResolveCurrentVersion();
            (CalVersion Version, string Tag, string AssetName, string AssetUrl)? latestRelease =
                await _findLatestMatchingReleaseAsync(
                    installationMetadata.ReleaseFlavor,
                    currentVersion,
                    cancellationToken).ConfigureAwait(false);

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
            InstallationMetadata installationMetadata = _installationMetadataRepository.Load();
            CalVersion currentVersion = ResolveCurrentVersion();
            (CalVersion Version, string Tag, string AssetName, string AssetUrl)? latestRelease =
                await _findLatestMatchingReleaseAsync(
                    installationMetadata.ReleaseFlavor,
                    currentVersion,
                    cancellationToken).ConfigureAwait(false);

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

            string stagedExecutablePath = await _downloadReleaseAssetAsync(
                installationMetadata,
                latestRelease.Value.AssetName,
                latestRelease.Value.AssetUrl,
                cancellationToken).ConfigureAwait(false);

            _installationMetadataRepository.EnsureHelperScript();
            PersistedUpdateRequest updateRequest = new()
            {
                ProcessId = Environment.ProcessId,
                DownloadedExecutablePath = stagedExecutablePath,
                InstalledExecutablePath = installationMetadata.InstalledExecutablePath,
                StartWithWindows = configuration.StartWithWindows,
                LaunchAfterApply = false
            };

            _installationMetadataRepository.SaveUpdateRequest(updateRequest);
            bool launchAfterApply = !TryLaunchRestartLauncher(installationMetadata.InstalledExecutablePath);

            if (launchAfterApply)
            {
                updateRequest = updateRequest with { LaunchAfterApply = true };
                _installationMetadataRepository.SaveUpdateRequest(updateRequest);
            }

            LaunchHelper(installationMetadata);
            _logPublisher.Write($"Update prepared for {latestRelease.Value.Tag} using asset {latestRelease.Value.AssetName}.");

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
        InstallationMetadata installationMetadata = _installationMetadataRepository.Load();
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

    public void Dispose()
    {
        _operationGate.Dispose();
    }

    internal static ProcessStartInfo BuildHelperProcessStartInfo(string shellPath, string helperScriptPath)
    {
        return new ProcessStartInfo
        {
            FileName = shellPath,
            Arguments = $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{helperScriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    internal static ProcessStartInfo BuildLauncherProcessStartInfo(
        string shellPath,
        string launcherScriptPath,
        string requestPath,
        string installedExecutablePath)
    {
        return new ProcessStartInfo
        {
            FileName = shellPath,
            Arguments =
                $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{launcherScriptPath}\" -RequestPath \"{requestPath}\" -InstalledPath \"{installedExecutablePath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
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
            _logPublisher.Write(
                $"Direct install-directory staging failed and is falling back to LocalAppData: {exception.Message}");
        }

        string fallbackDirectory = Path.Combine(_appPaths.DirectoryPath, "updates");
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

    private UpdateStatusSnapshot BuildEmptySnapshot()
    {
        InstallationMetadata installationMetadata = _installationMetadataRepository.Load();
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
            LastCheckedAtUtc = _timeProvider.GetUtcNow(),
            LastCheckErrorMessage = lastCheckErrorMessage
        };
    }

    private void LaunchHelper(InstallationMetadata installationMetadata)
    {
        ProcessStartInfo startInfo = installationMetadata.InstallationMode == InstallationMode.ProgramFiles
            && !string.IsNullOrWhiteSpace(installationMetadata.ElevatedTaskName)
            ? BuildScheduledTaskStartInfo(installationMetadata.ElevatedTaskName)
            : BuildHelperProcessStartInfo(
                InstallationMetadataRepository.ResolveShellExecutablePath(),
                _installationMetadataRepository.HelperScriptPath);

        _ = _processStarter(startInfo);
    }

    private static ProcessStartInfo BuildScheduledTaskStartInfo(string elevatedTaskName)
    {
        return new ProcessStartInfo
        {
            FileName = "schtasks.exe",
            Arguments = $"/Run /TN \"{elevatedTaskName}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        };
    }

    private static CalVersion ResolveCurrentVersion()
    {
        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? new CalVersion(0, 0, 0) : new CalVersion(version.Major, version.Minor, version.Build);
    }

    private bool TryLaunchRestartLauncher(string installedExecutablePath)
    {
        try
        {
            ProcessStartInfo startInfo = BuildLauncherProcessStartInfo(
                InstallationMetadataRepository.ResolveShellExecutablePath(),
                _installationMetadataRepository.LauncherScriptPath,
                _installationMetadataRepository.UpdateRequestPath,
                installedExecutablePath);
            Process? launcherProcess = _processStarter(startInfo);

            return launcherProcess is not null;
        }
        catch (Exception exception) when (
            exception is Win32Exception
            or FileNotFoundException)
        {
            _logPublisher.Write($"Restart launcher could not be started and helper relaunch will be used instead: {exception.Message}");
            return false;
        }
    }
}
