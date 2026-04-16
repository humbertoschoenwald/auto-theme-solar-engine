using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Features.Updates.Infrastructure;
using Xunit;

namespace SolarEngine.Tests.Features.Updates.Infrastructure;

/// <summary>
/// Verifies the generated updater scripts keep the update flow silent and deterministic.
/// </summary>
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
    /// Verifies the helper script performs a silent swap using the recorded install path.
    /// </summary>
    [Fact]
    public void EnsureHelperScript_WritesSilentReplacementWorkflow()
    {
        InstallationMetadataRepository repository = new(new AppPaths(_directoryPath));

        repository.EnsureHelperScript();

        string helperScript = File.ReadAllText(repository.HelperScriptPath);

        Assert.Contains("Wait-Process -Id $request.ProcessId -Timeout 15", helperScript, StringComparison.Ordinal);
        Assert.Contains("Start-Sleep -Seconds 2", helperScript, StringComparison.Ordinal);
        Assert.Contains("$downloadedPath = [string]$request.DownloadedExecutablePath", helperScript, StringComparison.Ordinal);
        Assert.Contains("$installedPath = [string]$request.InstalledExecutablePath", helperScript, StringComparison.Ordinal);
        Assert.Contains("Move-Item -LiteralPath $downloadedPath -Destination $installedPath -Force", helperScript, StringComparison.Ordinal);
        Assert.Contains("HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Run", helperScript, StringComparison.Ordinal);
        Assert.Contains("$runValueName = \"Solar Engine\"", helperScript, StringComparison.Ordinal);
        Assert.Contains("$legacyRunValueName = \"S\"", helperScript, StringComparison.Ordinal);
        Assert.Contains("auto-theme-solar-engine-win-x64-*.exe", helperScript, StringComparison.Ordinal);
        Assert.Contains("Start-Process -FilePath $installedPath", helperScript, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies the launcher script waits for the helper to finish before relaunching.
    /// </summary>
    [Fact]
    public void EnsureHelperScript_WritesLauncherScriptThatRelaunchesAfterApply()
    {
        InstallationMetadataRepository repository = new(new AppPaths(_directoryPath));

        repository.EnsureHelperScript();

        string launcherScript = File.ReadAllText(repository.LauncherScriptPath);

        Assert.Contains("$deadline = (Get-Date).AddMinutes(2)", launcherScript, StringComparison.Ordinal);
        Assert.Contains("Start-Sleep -Milliseconds 500", launcherScript, StringComparison.Ordinal);
        Assert.Contains("Start-Process -FilePath $InstalledPath", launcherScript, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies direct installs preserve the current release asset filename in installation metadata.
    /// </summary>
    [Fact]
    public void BuildPersistedInstallationMetadata_PreservesCurrentExecutableName()
    {
        PersistedInstallationMetadata metadata = InstallationMetadataRepository.BuildPersistedInstallationMetadata(
            @"C:\Program Files\Auto Theme — Solar Engine\auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe",
            ReleaseFlavor.SelfContained,
            InstallationMode.ProgramFiles,
            "Auto Theme Solar Engine Silent Update");

        Assert.Equal(
            "auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe",
            metadata.InstalledExecutableName);
        Assert.Equal("self-contained", metadata.ReleaseFlavor);
        Assert.Equal("program-files", metadata.InstallationMode);
        Assert.Equal("Auto Theme Solar Engine Silent Update", metadata.ElevatedTaskName);
    }

    /// <summary>
    /// Verifies the generated Program Files bootstrap registers an elevated task for the current user.
    /// </summary>
    [Fact]
    public void BuildElevatedTaskRegistrationScript_UsesCurrentHelperAndInteractivePrincipal()
    {
        string script = InstallationMetadataRepository.BuildElevatedTaskRegistrationScript(
            "Auto Theme Solar Engine Silent Update",
            @"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\Apply-SolarEngine-Update.ps1",
            @"C:\Program Files\PowerShell\7\pwsh.exe",
            @"MACHINE\tester");

        Assert.Contains("New-ScheduledTaskAction -Execute $shellPath -Argument $arguments", script, StringComparison.Ordinal);
        Assert.Contains("New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest", script, StringComparison.Ordinal);
        Assert.Contains(@"C:\Users\tester\AppData\Local\AutoThemeSolarEngine\Apply-SolarEngine-Update.ps1", script, StringComparison.Ordinal);
        Assert.Contains(@"C:\Program Files\PowerShell\7\pwsh.exe", script, StringComparison.Ordinal);
        Assert.Contains(@"MACHINE\tester", script, StringComparison.Ordinal);
        Assert.Contains("Register-ScheduledTask -TaskName 'Auto Theme Solar Engine Silent Update' -InputObject $task -Force | Out-Null", script, StringComparison.Ordinal);
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
