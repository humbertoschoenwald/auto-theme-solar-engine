// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Features.Updates.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Updates.Infrastructure;

/// <summary>
/// Verifies the generated updater scripts keep the update flow deterministic.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class InstallationMetadataRepositoryTests : IDisposable
{
    private readonly string _directoryPath = Path.Combine(
        Path.GetTempPath(),
        "SolarEngine.Tests",
        Path.GetRandomFileName());

    /// <summary>
    /// Creates an isolated application data root for updater script generation tests.
    /// </summary>
    public InstallationMetadataRepositoryTests()
    {
        _ = Directory.CreateDirectory(_directoryPath);
    }

    /// <summary>
    /// Verifies the helper script performs a visible swap using the recorded install path.
    /// </summary>
    [Fact]
    public void EnsureHelperScriptWritesVisibleReplacementWorkflow()
    {
        InstallationMetadataRepository repository = new(new AppPaths(_directoryPath));

        repository.EnsureHelperScript();

        string helperScript = File.ReadAllText(repository.HelperScriptPath);

        Assert.Contains("Write-Host \"Waiting for AutoThemeSolarEngine to close...\"", helperScript, StringComparison.Ordinal);
        Assert.Contains("Wait-Process -Id $request.ProcessId -Timeout 15", helperScript, StringComparison.Ordinal);
        Assert.Contains("Start-Sleep -Seconds 2", helperScript, StringComparison.Ordinal);
        Assert.Contains(repository.UpdateRequestPath, helperScript, StringComparison.Ordinal);
        Assert.Contains("$downloadedPath = [string]$request.DownloadedExecutablePath", helperScript, StringComparison.Ordinal);
        Assert.Contains("$installedPath = [string]$request.InstalledExecutablePath", helperScript, StringComparison.Ordinal);
        Assert.Contains("Move-Item -LiteralPath $downloadedPath -Destination $installedPath -Force", helperScript, StringComparison.Ordinal);
        Assert.Contains("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", helperScript, StringComparison.Ordinal);
        Assert.Contains("$runValueName = \"AutoThemeSolarEngine\"", helperScript, StringComparison.Ordinal);
        Assert.Contains("$legacyRunValueName = \"S\"", helperScript, StringComparison.Ordinal);
        Assert.Contains("auto-theme-solar-engine-win-x64-*.exe", helperScript, StringComparison.Ordinal);
        Assert.Contains("Start-Process -FilePath $installedPath", helperScript, StringComparison.Ordinal);
        Assert.DoesNotContain("LaunchAfterApply", helperScript, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies compatibility installs outside the LocalAppData model preserve the current executable name.
    /// </summary>
    [Fact]
    public void BuildPersistedInstallationMetadataPreservesProgramFilesExecutableName()
    {
        PersistedInstallationMetadata metadata = InstallationMetadataRepository.BuildPersistedInstallationMetadata(
            @"C:\Program Files\AutoThemeSolarEngine\auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
            ReleaseFlavor.SelfContained,
            InstallationMode.ProgramFiles,
            "AutoThemeSolarEngine Update");

        Assert.Equal(
            "auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
            metadata.InstalledExecutableName);
        Assert.Equal("self-contained", metadata.ReleaseFlavor);
        Assert.Equal("program-files", metadata.InstallationMode);
        Assert.Equal("AutoThemeSolarEngine Update", metadata.ElevatedTaskName);
    }

    /// <summary>
    /// Verifies documented LocalAppData installs normalize to the stable executable target.
    /// </summary>
    [Fact]
    public void BuildPersistedInstallationMetadataUsesStableExecutableNameForLocalAppData()
    {
        PersistedInstallationMetadata metadata = InstallationMetadataRepository.BuildPersistedInstallationMetadata(
            @"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe",
            ReleaseFlavor.SelfContained,
            InstallationMode.LocalAppData,
            elevatedTaskName: null);

        Assert.Equal("AutoThemeSolarEngine.exe", metadata.InstalledExecutableName);
        Assert.Equal("self-contained", metadata.ReleaseFlavor);
        Assert.Equal("local-app-data", metadata.InstallationMode);
        Assert.Null(metadata.ElevatedTaskName);
    }

    /// <summary>
    /// Verifies legacy LocalAppData metadata migrates to the stable executable target.
    /// </summary>
    [Fact]
    public void NormalizePersistedInstallationMetadataMigratesLegacyLocalAppDataExecutableName()
    {
        PersistedInstallationMetadata metadata = InstallationMetadataRepository.NormalizePersistedInstallationMetadata(
            @"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
            new PersistedInstallationMetadata
            {
                InstalledExecutableName = "auto-theme-solar-engine-win-x64-self-contained-v26.04.04.exe",
                ReleaseFlavor = "self-contained",
                InstallationMode = "local-app-data"
            },
            ReleaseFlavor.SelfContained,
            InstallationMode.LocalAppData,
            elevatedTaskName: null);

        Assert.Equal("AutoThemeSolarEngine.exe", metadata.InstalledExecutableName);
        Assert.Equal("self-contained", metadata.ReleaseFlavor);
        Assert.Equal("local-app-data", metadata.InstallationMode);
    }

    /// <summary>
    /// Verifies legacy manifests that still declare the removed framework-dependent flavor normalize to self-contained.
    /// </summary>
    [Fact]
    public void NormalizePersistedInstallationMetadataNormalizesLegacyFrameworkDependentFlavor()
    {
        PersistedInstallationMetadata metadata = InstallationMetadataRepository.NormalizePersistedInstallationMetadata(
            @"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.04.exe",
            new PersistedInstallationMetadata
            {
                InstalledExecutableName = "auto-theme-solar-engine-win-x64-framework-dependent-v26.04.04.exe",
                ReleaseFlavor = "framework-dependent",
                InstallationMode = "local-app-data"
            },
            ReleaseFlavor.FrameworkDependent,
            InstallationMode.LocalAppData,
            elevatedTaskName: null);

        Assert.Equal("AutoThemeSolarEngine.exe", metadata.InstalledExecutableName);
        Assert.Equal("self-contained", metadata.ReleaseFlavor);
        Assert.Equal("local-app-data", metadata.InstallationMode);
    }

    /// <summary>
    /// Verifies the generated Program Files bootstrap registers an elevated task for the current user.
    /// </summary>
    [Fact]
    public void BuildElevatedTaskRegistrationScriptUsesCurrentHelperAndInteractivePrincipal()
    {
        string script = InstallationMetadataRepository.BuildElevatedTaskRegistrationScript(
            "AutoThemeSolarEngine Update",
            @"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\Apply-SolarEngine-Update.ps1",
            @"C:\Program Files\PowerShell\7\pwsh.exe",
            @"MACHINE\tester");

        Assert.Contains("New-ScheduledTaskAction -Execute $shellPath -Argument $arguments", script, StringComparison.Ordinal);
        Assert.Contains("New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest", script, StringComparison.Ordinal);
        Assert.Contains(@"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\Apply-SolarEngine-Update.ps1", script, StringComparison.Ordinal);
        Assert.Contains(@"C:\Program Files\PowerShell\7\pwsh.exe", script, StringComparison.Ordinal);
        Assert.Contains(@"MACHINE\tester", script, StringComparison.Ordinal);
        Assert.Contains("Register-ScheduledTask -TaskName 'AutoThemeSolarEngine Update' -InputObject $task -Force | Out-Null", script, StringComparison.Ordinal);
        Assert.DoesNotContain("-WindowStyle\", \"Hidden", script, StringComparison.Ordinal);
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
