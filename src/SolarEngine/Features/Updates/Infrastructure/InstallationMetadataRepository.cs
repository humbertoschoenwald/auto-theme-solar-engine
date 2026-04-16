using System.Text;
using System.Text.Json;
using SolarEngine.Features.SystemHost.Domain;
using SolarEngine.Features.Updates.Domain;
using SolarEngine.Shared;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Updates.Infrastructure;

internal sealed class InstallationMetadataRepository(AppPaths appPaths)
{
    private const string ManifestFileName = "installation.json";

    public string HelperScriptPath =>
        Path.Combine(appPaths.DirectoryPath, $"Apply-{AppIdentity.RuntimeFileStem}-Update.ps1");

    public string LauncherScriptPath =>
        Path.Combine(appPaths.DirectoryPath, $"Launch-{AppIdentity.RuntimeFileStem}-After-Update.ps1");

    public string UpdateRequestPath => Path.Combine(appPaths.DirectoryPath, "update-request.json");

    public InstallationMetadata Load()
    {
        string processPath = Environment.ProcessPath
            ?? throw new UnexpectedStateException("Resolve the current executable path before loading installation metadata.");
        string installDirectory = Path.GetDirectoryName(processPath)
            ?? throw new UnexpectedStateException("Resolve the current executable directory before loading installation metadata.");
        string manifestPath = Path.Combine(installDirectory, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            return InferFromProcess(processPath, installDirectory);
        }

        using FileStream stream = new(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        PersistedInstallationMetadata? persistedMetadata = JsonSerializer.Deserialize(
            stream,
            UpdateJsonContext.Default.PersistedInstallationMetadata);

        return persistedMetadata is null
            ? InferFromProcess(processPath, installDirectory)
            : new InstallationMetadata
            {
                InstallDirectory = installDirectory,
                InstalledExecutablePath = Path.Combine(
                    installDirectory,
                    string.IsNullOrWhiteSpace(persistedMetadata.InstalledExecutableName)
                        ? Path.GetFileName(processPath)
                        : persistedMetadata.InstalledExecutableName),
                ReleaseFlavor = ParseReleaseFlavor(persistedMetadata.ReleaseFlavor, processPath),
                InstallationMode = ParseInstallationMode(persistedMetadata.InstallationMode, installDirectory),
                ElevatedTaskName = persistedMetadata.ElevatedTaskName
            };
    }

    public void SaveUpdateRequest(PersistedUpdateRequest request)
    {
        _ = Directory.CreateDirectory(appPaths.DirectoryPath);
        using FileStream stream = new(UpdateRequestPath, FileMode.Create, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, request, UpdateJsonContext.Default.PersistedUpdateRequest);
        stream.Flush(flushToDisk: true);
    }

    public void EnsureHelperScript()
    {
        _ = Directory.CreateDirectory(appPaths.DirectoryPath);
        File.WriteAllText(HelperScriptPath, BuildHelperScript(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(LauncherScriptPath, BuildLauncherScript(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static InstallationMetadata InferFromProcess(string processPath, string installDirectory)
    {
        return new InstallationMetadata
        {
            InstallDirectory = installDirectory,
            InstalledExecutablePath = processPath,
            ReleaseFlavor = InferReleaseFlavor(processPath),
            InstallationMode = InferInstallationMode(installDirectory),
            ElevatedTaskName = null
        };
    }

    private static ReleaseFlavor ParseReleaseFlavor(string? value, string processPath)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "self-contained" => ReleaseFlavor.SelfContained,
            "framework-dependent" => ReleaseFlavor.FrameworkDependent,
            _ => InferReleaseFlavor(processPath)
        };
    }

    private static InstallationMode ParseInstallationMode(string? value, string installDirectory)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "local-app-data" => InstallationMode.LocalAppData,
            "program-files" => InstallationMode.ProgramFiles,
            _ => InferInstallationMode(installDirectory)
        };
    }

    private static ReleaseFlavor InferReleaseFlavor(string processPath)
    {
        string fileName = Path.GetFileName(processPath);
        return fileName.Contains("framework-dependent", StringComparison.OrdinalIgnoreCase)
            ? ReleaseFlavor.FrameworkDependent
            : fileName.Contains("self-contained", StringComparison.OrdinalIgnoreCase)
                ? ReleaseFlavor.SelfContained
                : ReleaseFlavor.SelfContained;
    }

    private static InstallationMode InferInstallationMode(string installDirectory)
    {
        string normalizedDirectory = Path.GetFullPath(installDirectory);
        string programFiles = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        string localAppData = Path.GetFullPath(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create));

        if (normalizedDirectory.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase))
        {
            return InstallationMode.ProgramFiles;
        }

        return normalizedDirectory.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase)
            ? InstallationMode.LocalAppData
            : InstallationMode.Unknown;
    }

    private static string BuildHelperScript()
    {
        return $$"""
$ErrorActionPreference = "Stop"

$requestPath = Join-Path $env:LOCALAPPDATA "AutoThemeSolarEngine\update-request.json"
if (-not (Test-Path -LiteralPath $requestPath)) {
  exit 0
}

$request = Get-Content -LiteralPath $requestPath -Raw | ConvertFrom-Json

if ($request.ProcessId -gt 0) {
  try {
    Wait-Process -Id $request.ProcessId -Timeout 15 -ErrorAction Stop
  }
  catch {
    Stop-Process -Id $request.ProcessId -Force -ErrorAction SilentlyContinue
  }
}

Start-Sleep -Seconds 2

$downloadedPath = [string]$request.DownloadedExecutablePath
$installedPath = [string]$request.InstalledExecutablePath

if (-not (Test-Path -LiteralPath $downloadedPath)) {
  Remove-Item -LiteralPath $requestPath -ErrorAction SilentlyContinue
  exit 1
}

$installedDirectory = Split-Path -LiteralPath $installedPath -Parent
New-Item -ItemType Directory -Path $installedDirectory -Force | Out-Null

if (Test-Path -LiteralPath $installedPath) {
  Remove-Item -LiteralPath $installedPath -Force
}

Move-Item -LiteralPath $downloadedPath -Destination $installedPath -Force

Get-ChildItem -LiteralPath $installedDirectory -Filter "auto-theme-solar-engine-win-x64-*.exe" -ErrorAction SilentlyContinue |
  Where-Object { -not [System.StringComparer]::OrdinalIgnoreCase.Equals($_.FullName, $installedPath) } |
  Remove-Item -Force -ErrorAction SilentlyContinue

$runKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$runValueName = "{{AppIdentity.RuntimeName}}"
$legacyRunValueName = "{{AppIdentity.LegacyRuntimeName}}"
if ($request.StartWithWindows) {
  Set-ItemProperty -Path $runKeyPath -Name $runValueName -Value ('"{0}"' -f $installedPath)
  Remove-ItemProperty -Path $runKeyPath -Name $legacyRunValueName -ErrorAction SilentlyContinue
}
else {
  Remove-ItemProperty -Path $runKeyPath -Name $runValueName -ErrorAction SilentlyContinue
  Remove-ItemProperty -Path $runKeyPath -Name $legacyRunValueName -ErrorAction SilentlyContinue
}

Remove-Item -LiteralPath $requestPath -ErrorAction SilentlyContinue

if ($request.LaunchAfterApply) {
  Start-Process -FilePath $installedPath
}
""";
    }

    private static string BuildLauncherScript()
    {
        return """
$ErrorActionPreference = "Stop"

param(
  [Parameter(Mandatory = $true)]
  [string]$RequestPath,

  [Parameter(Mandatory = $true)]
  [string]$InstalledPath
)

$deadline = (Get-Date).AddMinutes(2)

while ((Get-Date) -lt $deadline) {
  if (-not (Test-Path -LiteralPath $RequestPath) -and (Test-Path -LiteralPath $InstalledPath)) {
    Start-Process -FilePath $InstalledPath
    exit 0
  }

  Start-Sleep -Milliseconds 500
}

exit 1
""";
    }
}
