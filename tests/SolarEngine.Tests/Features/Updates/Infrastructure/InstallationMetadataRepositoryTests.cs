using SolarEngine.Features.SystemHost.Domain;
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
