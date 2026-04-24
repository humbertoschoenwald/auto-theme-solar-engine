// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using Microsoft.Win32;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates;
using SolarEngine.Features.Updates.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Updates.Infrastructure;

/// <summary>
/// Verifies the generated updater scripts rehearse legacy migration and relaunch behavior end to end.
/// </summary>
[Collection("UpdaterScripts")]
[Trait("TestLane", "Heavy")]
public sealed class UpdateScriptIntegrationTests : IDisposable
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupValueName = "AutoThemeSolarEngine";
    private const string LegacyStartupValueName = "S";

    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "SolarEngine.Tests",
        Path.GetRandomFileName());

    /// <summary>
    /// Creates an isolated application data root for updater script integration tests.
    /// </summary>
    public UpdateScriptIntegrationTests()
    {
        _ = Directory.CreateDirectory(_directoryPath);
    }

    /// <summary>
    /// Verifies the helper migrates a legacy LocalAppData layout to the stable executable and relaunches it.
    /// </summary>
    [Fact]
    public async Task HelperScriptMigratesLegacyLayoutAndRelaunchesStableExecutable()
    {
        AppPaths appPaths = new(Path.Combine(_directoryPath, "app-data"));
        InstallationMetadataRepository repository = new(appPaths);
        string installDirectory = Path.Combine(_directoryPath, "install");
        string stableInstalledPath = Path.Combine(installDirectory, "AutoThemeSolarEngine.exe");
        string legacyInstalledPath = Path.Combine(
            installDirectory,
            "auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe");
        string staleInstalledPath = Path.Combine(
            installDirectory,
            "auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe");
        string downloadedPath = Path.Combine(
            installDirectory,
            "auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe");
        string markerPath = Path.Combine(_directoryPath, "helper-launch.txt");

        repository.EnsureHelperScript();
        _ = Directory.CreateDirectory(installDirectory);
        CopyProbeExecutable(legacyInstalledPath);
        CopyProbeExecutable(staleInstalledPath);
        CopyProbeExecutable(downloadedPath);

        repository.SaveUpdateRequest(new PersistedUpdateRequest
        {
            ProcessId = 0,
            DownloadedExecutablePath = downloadedPath,
            InstalledExecutablePath = stableInstalledPath,
            StartWithWindows = true,
            LaunchAfterApply = true
        });

        RegistryValueSnapshot startupSnapshot = CaptureRunValue(StartupValueName);
        RegistryValueSnapshot legacySnapshot = CaptureRunValue(LegacyStartupValueName);

        try
        {
            ScriptRunResult result = await RunScriptAsync(repository.HelperScriptPath, markerPath);

            Assert.True(
                result.ExitCode == 0,
                $"Helper script failed with exit code {result.ExitCode}.{Environment.NewLine}STDOUT:{Environment.NewLine}{result.StandardOutput}{Environment.NewLine}STDERR:{Environment.NewLine}{result.StandardError}");
            await WaitForFileAsync(markerPath);
            Assert.True(File.Exists(stableInstalledPath));
            Assert.False(File.Exists(downloadedPath));
            Assert.False(File.Exists(legacyInstalledPath));
            Assert.False(File.Exists(staleInstalledPath));
            Assert.False(File.Exists(repository.UpdateRequestPath));
            Assert.Equal(
                $"\"{stableInstalledPath}\"",
                ReadRunValue(StartupValueName));
            Assert.Null(ReadRunValue(LegacyStartupValueName));
        }
        finally
        {
            RestoreRunValue(StartupValueName, startupSnapshot);
            RestoreRunValue(LegacyStartupValueName, legacySnapshot);
        }
    }

    /// <summary>
    /// Verifies the relaunch watcher waits for the request to disappear before starting the updated executable.
    /// </summary>
    [Fact]
    public async Task LauncherScriptWaitsForRequestRemovalBeforeLaunchingStableExecutable()
    {
        AppPaths appPaths = new(Path.Combine(_directoryPath, "app-data"));
        InstallationMetadataRepository repository = new(appPaths);
        string installDirectory = Path.Combine(_directoryPath, "install");
        string installedPath = Path.Combine(installDirectory, "AutoThemeSolarEngine.exe");
        string markerPath = Path.Combine(_directoryPath, "launcher-launch.txt");

        repository.EnsureHelperScript();
        _ = Directory.CreateDirectory(installDirectory);
        CopyProbeExecutable(installedPath);
        File.WriteAllText(repository.UpdateRequestPath, "{}");

        ProcessStartInfo startInfo = UpdateCoordinator.BuildLauncherProcessStartInfo(
            InstallationMetadataRepository.ResolveShellExecutablePath(),
            repository.LauncherScriptPath,
            repository.UpdateRequestPath,
            installedPath);
        ApplyRepositoryDotNetEnvironment(startInfo);
        startInfo.Environment["SOLAR_ENGINE_UPDATE_PROBE_MARKER_PATH"] = markerPath;

        using Process launcherProcess = Process.Start(startInfo)
            ?? throw new Xunit.Sdk.XunitException("Start the launcher script before asserting relaunch behavior.");

        await Task.Delay(TimeSpan.FromSeconds(1));
        Assert.False(File.Exists(markerPath));

        File.Delete(repository.UpdateRequestPath);
        await launcherProcess.WaitForExitAsync();

        Assert.Equal(0, launcherProcess.ExitCode);
        await WaitForFileAsync(markerPath);
    }

    /// <summary>
    /// Removes the isolated application data root after each test.
    /// </summary>
    public void Dispose()
    {
        for (int attempt = 0; attempt < 10; attempt++)
        {
            if (!Directory.Exists(_directoryPath))
            {
                return;
            }

            try
            {
                Directory.Delete(_directoryPath, recursive: true);
                return;
            }
            catch (Exception exception) when (
                exception is IOException
                or UnauthorizedAccessException)
            {
                Thread.Sleep(200);
            }
        }
    }

    private static RegistryValueSnapshot CaptureRunValue(string valueName)
    {
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        object? value = runKey.GetValue(valueName);
        RegistryValueKind? valueKind = value is null ? null : runKey.GetValueKind(valueName);

        return new RegistryValueSnapshot(value, valueKind);
    }

    private static void RestoreRunValue(string valueName, RegistryValueSnapshot snapshot)
    {
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (snapshot.ValueKind is null)
        {
            runKey.DeleteValue(valueName, throwOnMissingValue: false);
            return;
        }

        runKey.SetValue(valueName, snapshot.Value ?? string.Empty, snapshot.ValueKind.Value);
    }

    private static string? ReadRunValue(string valueName)
    {
        using RegistryKey runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        return runKey.GetValue(valueName) as string;
    }

    private static void CopyProbeExecutable(string destinationPath)
    {
        string sourcePath = ResolveProbeExecutablePath();
        string destinationDirectory = Path.GetDirectoryName(destinationPath)
            ?? throw new Xunit.Sdk.XunitException("Resolve the probe destination directory before copying the rehearsal executable.");
        string sourceDirectory = Path.GetDirectoryName(sourcePath)
            ?? throw new Xunit.Sdk.XunitException("Resolve the probe source directory before copying the rehearsal executable.");

        _ = Directory.CreateDirectory(destinationDirectory);
        foreach (string sourceFilePath in Directory.GetFiles(sourceDirectory))
        {
            if (string.Equals(sourceFilePath, sourcePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string destinationFilePath = Path.Combine(destinationDirectory, Path.GetFileName(sourceFilePath));
            File.Copy(sourceFilePath, destinationFilePath, overwrite: true);
        }

        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static string ResolveProbeExecutablePath()
    {
        string repositoryRoot = ResolveRepositoryRoot();
        string configuration = AppContext.BaseDirectory.Contains(
            $"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}",
            StringComparison.OrdinalIgnoreCase)
            ? "Release"
            : "Debug";
        string executablePath = Path.Combine(
            repositoryRoot,
            "tests",
            "SolarEngine.UpdateProbe",
            "bin",
            configuration,
            "net11.0-windows10.0.19041.0",
            "SolarEngine.UpdateProbe.exe");

        return File.Exists(executablePath)
            ? executablePath
            : throw new FileNotFoundException("Build the update probe executable before running updater integration tests.", executablePath);
    }

    private static async Task<ScriptRunResult> RunScriptAsync(string scriptPath, string markerPath)
    {
        ProcessStartInfo startInfo = UpdateCoordinator.BuildHelperProcessStartInfo(
            InstallationMetadataRepository.ResolveShellExecutablePath(),
            scriptPath);
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        ApplyRepositoryDotNetEnvironment(startInfo);
        startInfo.Environment["SOLAR_ENGINE_UPDATE_PROBE_MARKER_PATH"] = markerPath;

        using Process process = Process.Start(startInfo)
            ?? throw new Xunit.Sdk.XunitException("Start the helper script before asserting update behavior.");
        string standardOutput = await process.StandardOutput.ReadToEndAsync();
        string standardError = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return new ScriptRunResult(process.ExitCode, standardOutput, standardError);
    }

    private static async Task WaitForFileAsync(string path)
    {
        TimeSpan timeout = TimeSpan.FromSeconds(10);
        DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        throw new TimeoutException($"Timed out waiting for file '{path}'.");
    }

    private static void ApplyRepositoryDotNetEnvironment(ProcessStartInfo startInfo)
    {
        string dotNetRoot = Path.Combine(ResolveRepositoryRoot(), ".dotnet");
        if (!Directory.Exists(dotNetRoot))
        {
            throw new DirectoryNotFoundException("Resolve the repository-local .NET runtime before running updater integration tests.");
        }

        startInfo.Environment["DOTNET_ROOT"] = dotNetRoot;
        startInfo.Environment["DOTNET_ROOT_X64"] = dotNetRoot;
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

        throw new DirectoryNotFoundException("Resolve the repository root before running updater integration tests.");
    }

    private readonly record struct ScriptRunResult(int ExitCode, string StandardOutput, string StandardError);
    private readonly record struct RegistryValueSnapshot(object? Value, RegistryValueKind? ValueKind);
}
