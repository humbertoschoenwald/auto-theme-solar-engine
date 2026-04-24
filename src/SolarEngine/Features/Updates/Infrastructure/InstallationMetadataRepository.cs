// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Diagnostics;
using System.Security.Principal;
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
    private const string ElevatedUpdateTaskName = "AutoThemeSolarEngine Silent Update";
    private const string UpdateRequestFileName = "update-request.json";
    private const string SelfContainedFlavorName = "self-contained";
    private const string FrameworkDependentFlavorName = "framework-dependent";
    private const string LocalAppDataInstallationModeName = "local-app-data";
    private const string ProgramFilesInstallationModeName = "program-files";
    private const string UnknownInstallationModeName = "unknown";
    private const string CurrentExecutablePathDescription = "Resolve the current executable path before registering installation metadata.";
    private const string CurrentExecutableDirectoryDescription = "Resolve the current executable directory before registering installation metadata.";
    private const string InstallationMetadataLoadPathDescription = "Resolve the current executable path before loading installation metadata.";
    private const string InstallationMetadataDirectoryLoadDescription = "Resolve the current executable directory before loading installation metadata.";
    private const string CurrentWindowsIdentityDescription = "Resolve the current Windows identity before registering the elevated updater task.";
    private const string StartRegistrationProcessDescription = "Start the elevated updater task registration process before treating this install as update-ready.";
    private const string RegistrationProcessExitDescription = "Register the elevated updater task before treating this install as update-ready.";
    private const string PowerShellResolutionDescription = "Resolve a PowerShell executable before registering the elevated updater task.";
    private const string ProgramFilesDirectoryName = "PowerShell";
    private const string PowerShellSevenDirectoryName = "7";
    private const string PowerShellSevenExecutableName = "pwsh.exe";
    private const string WindowsPowerShellRelativePath = @"WindowsPowerShell\v1.0\powershell.exe";
    private const string NoLogoArgument = "-NoLogo";
    private const string NoProfileArgument = "-NoProfile";
    private const string NonInteractiveArgument = "-NonInteractive";
    private const string ExecutionPolicyArgument = "-ExecutionPolicy";
    private const string BypassExecutionPolicy = "Bypass";
    private const string EncodedCommandArgument = "-EncodedCommand";
    private const string CommandArgumentSeparator = " ";
    private const int SuccessExitCode = 0;
    private const string ElevatedTaskRegistrationHelperScriptToken = "__HELPER_SCRIPT_PATH__";
    private const string ElevatedTaskRegistrationShellPathToken = "__SHELL_PATH__";
    private const string ElevatedTaskRegistrationUserIdToken = "__USER_ID__";
    private const string ElevatedTaskRegistrationTaskNameToken = "__TASK_NAME__";
    private const string HelperScriptRequestPathToken = "__REQUEST_PATH__";
    private const string LauncherScriptStartupValueNameToken = "__STARTUP_VALUE_NAME__";
    private const string LauncherScriptLegacyStartupValueNameToken = "__LEGACY_STARTUP_VALUE_NAME__";
    private const string SingleQuote = "'";

    private const string ElevatedTaskRegistrationScriptTemplate = """
$ErrorActionPreference = "Stop"

$helperScriptPath = __HELPER_SCRIPT_PATH__
$shellPath = __SHELL_PATH__
$userId = __USER_ID__
$arguments = @(
  "-NoLogo",
  "-NoProfile",
  "-NonInteractive",
  "-ExecutionPolicy", "Bypass",
  "-WindowStyle", "Hidden",
  "-File", ('"{0}"' -f $helperScriptPath)
) -join " "

$action = New-ScheduledTaskAction -Execute $shellPath -Argument $arguments
$principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest
$settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
$task = New-ScheduledTask -Action $action -Principal $principal -Settings $settings
Register-ScheduledTask -TaskName __TASK_NAME__ -InputObject $task -Force | Out-Null
""";

    private const string HelperScriptTemplate = """
$ErrorActionPreference = "Stop"

$requestPath = __REQUEST_PATH__
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

$installedDirectory = Split-Path -Path $installedPath -Parent
New-Item -ItemType Directory -Path $installedDirectory -Force | Out-Null

if (Test-Path -LiteralPath $installedPath) {
  Remove-Item -LiteralPath $installedPath -Force
}

Move-Item -LiteralPath $downloadedPath -Destination $installedPath -Force

Get-ChildItem -LiteralPath $installedDirectory -Filter "auto-theme-solar-engine-win-x64-*.exe" -ErrorAction SilentlyContinue |
  Where-Object { -not [System.StringComparer]::OrdinalIgnoreCase.Equals($_.FullName, $installedPath) } |
  Remove-Item -Force -ErrorAction SilentlyContinue

$runKeyPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$runValueName = "__STARTUP_VALUE_NAME__"
$legacyRunValueName = "__LEGACY_STARTUP_VALUE_NAME__"
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

    private const string LauncherScriptTemplate = """
param(
  [Parameter(Mandatory = $true)]
  [string]$RequestPath,

  [Parameter(Mandatory = $true)]
  [string]$InstalledPath
)

$ErrorActionPreference = "Stop"

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

    public string HelperScriptPath =>
        Path.Combine(appPaths.DirectoryPath, $"Apply-{AppIdentity.RuntimeFileStem}-Update.ps1");

    public string LauncherScriptPath =>
        Path.Combine(appPaths.DirectoryPath, $"Launch-{AppIdentity.RuntimeFileStem}-After-Update.ps1");

    public string UpdateRequestPath => Path.Combine(appPaths.DirectoryPath, UpdateRequestFileName);

    public void EnsureCurrentInstallationRegistered()
    {
        string processPath = Environment.ProcessPath
            ?? throw new UnexpectedStateException(CurrentExecutablePathDescription);
        string installDirectory = Path.GetDirectoryName(processPath)
            ?? throw new UnexpectedStateException(CurrentExecutableDirectoryDescription);
        string manifestPath = Path.Combine(installDirectory, ManifestFileName);
        ReleaseFlavor releaseFlavor = InferReleaseFlavor(processPath);
        InstallationMode installationMode = InferInstallationMode(installDirectory);

        EnsureHelperScript();

        PersistedInstallationMetadata metadata = File.Exists(manifestPath)
            ? LoadPersistedInstallationMetadata(manifestPath)
                ?? BuildPersistedInstallationMetadata(
                    processPath,
                    releaseFlavor,
                    installationMode,
                    elevatedTaskName: null)
            : BuildPersistedInstallationMetadata(
                processPath,
                releaseFlavor,
                installationMode,
                elevatedTaskName: null);
        string? elevatedTaskName = metadata.ElevatedTaskName;

        if (installationMode == InstallationMode.ProgramFiles
            && string.IsNullOrWhiteSpace(elevatedTaskName))
        {
            if (!IsCurrentProcessElevated())
            {
                elevatedTaskName = null;
            }
            else
            {
                RegisterElevatedUpdateTask(ElevatedUpdateTaskName, HelperScriptPath);
                elevatedTaskName = ElevatedUpdateTaskName;
            }
        }

        PersistedInstallationMetadata normalizedMetadata = NormalizePersistedInstallationMetadata(
            processPath,
            metadata,
            releaseFlavor,
            installationMode,
            elevatedTaskName);

        WritePersistedInstallationMetadata(manifestPath, normalizedMetadata);
    }

    public static InstallationMetadata Load()
    {
        string processPath = Environment.ProcessPath
            ?? throw new UnexpectedStateException(InstallationMetadataLoadPathDescription);
        string installDirectory = Path.GetDirectoryName(processPath)
            ?? throw new UnexpectedStateException(InstallationMetadataDirectoryLoadDescription);
        string manifestPath = Path.Combine(installDirectory, ManifestFileName);

        PersistedInstallationMetadata? persistedMetadata = File.Exists(manifestPath)
            ? LoadPersistedInstallationMetadata(manifestPath)
            : null;

        if (persistedMetadata is null)
        {
            return InferFromProcess(processPath, installDirectory);
        }

        InstallationMode installationMode = ParseInstallationMode(persistedMetadata.InstallationMode, installDirectory);
        string installedExecutableName = ResolveInstalledExecutableName(
            string.IsNullOrWhiteSpace(persistedMetadata.InstalledExecutableName)
                ? Path.GetFileName(processPath)
                : persistedMetadata.InstalledExecutableName,
            installationMode);

        return new InstallationMetadata
        {
            InstallDirectory = installDirectory,
            InstalledExecutablePath = Path.Combine(installDirectory, installedExecutableName),
            ReleaseFlavor = ParseReleaseFlavor(persistedMetadata.ReleaseFlavor, processPath),
            InstallationMode = installationMode,
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
        File.WriteAllText(HelperScriptPath, BuildHelperScript(UpdateRequestPath), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(LauncherScriptPath, BuildLauncherScript(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    internal static PersistedInstallationMetadata BuildPersistedInstallationMetadata(
        string processPath,
        ReleaseFlavor releaseFlavor,
        InstallationMode installationMode,
        string? elevatedTaskName)
    {
        return new PersistedInstallationMetadata
        {
            InstalledExecutableName = ResolveInstalledExecutableName(
                Path.GetFileName(processPath),
                installationMode),
            ReleaseFlavor = SerializeReleaseFlavor(releaseFlavor),
            InstallationMode = SerializeInstallationMode(installationMode),
            ElevatedTaskName = elevatedTaskName
        };
    }

    internal static PersistedInstallationMetadata NormalizePersistedInstallationMetadata(
        string processPath,
        PersistedInstallationMetadata persistedMetadata,
        ReleaseFlavor releaseFlavor,
        InstallationMode installationMode,
        string? elevatedTaskName)
    {
        string currentExecutableName = string.IsNullOrWhiteSpace(persistedMetadata.InstalledExecutableName)
            ? Path.GetFileName(processPath)
            : persistedMetadata.InstalledExecutableName;

        return persistedMetadata with
        {
            InstalledExecutableName = ResolveInstalledExecutableName(currentExecutableName, installationMode),
            ReleaseFlavor = SerializeReleaseFlavor(releaseFlavor),
            InstallationMode = SerializeInstallationMode(installationMode),
            ElevatedTaskName = elevatedTaskName
        };
    }

    internal static string BuildElevatedTaskRegistrationScript(
        string taskName,
        string helperScriptPath,
        string shellPath,
        string userId)
    {
        return ElevatedTaskRegistrationScriptTemplate
            .Replace(ElevatedTaskRegistrationHelperScriptToken, ToPowerShellLiteral(helperScriptPath), StringComparison.Ordinal)
            .Replace(ElevatedTaskRegistrationShellPathToken, ToPowerShellLiteral(shellPath), StringComparison.Ordinal)
            .Replace(ElevatedTaskRegistrationUserIdToken, ToPowerShellLiteral(userId), StringComparison.Ordinal)
            .Replace(ElevatedTaskRegistrationTaskNameToken, ToPowerShellLiteral(taskName), StringComparison.Ordinal);
    }

    private static InstallationMetadata InferFromProcess(string processPath, string installDirectory)
    {
        InstallationMode installationMode = InferInstallationMode(installDirectory);

        return new InstallationMetadata
        {
            InstallDirectory = installDirectory,
            InstalledExecutablePath = Path.Combine(
                installDirectory,
                ResolveInstalledExecutableName(Path.GetFileName(processPath), installationMode)),
            ReleaseFlavor = InferReleaseFlavor(processPath),
            InstallationMode = installationMode,
            ElevatedTaskName = null
        };
    }

    private static ReleaseFlavor ParseReleaseFlavor(string? value, string processPath)
    {
        _ = processPath;

        string? normalizedValue = value?.Trim();
        return string.Equals(normalizedValue, SelfContainedFlavorName, StringComparison.OrdinalIgnoreCase)
            ? ReleaseFlavor.SelfContained
            : string.Equals(normalizedValue, FrameworkDependentFlavorName, StringComparison.OrdinalIgnoreCase)
            ? ReleaseFlavor.SelfContained
            : ReleaseFlavor.SelfContained;
    }

    private static InstallationMode ParseInstallationMode(string? value, string installDirectory)
    {
        string? normalizedValue = value?.Trim();
        return string.Equals(normalizedValue, LocalAppDataInstallationModeName, StringComparison.OrdinalIgnoreCase)
            ? InstallationMode.LocalAppData
            : string.Equals(normalizedValue, ProgramFilesInstallationModeName, StringComparison.OrdinalIgnoreCase)
            ? InstallationMode.ProgramFiles
            : InferInstallationMode(installDirectory);
    }

    private static ReleaseFlavor InferReleaseFlavor(string processPath)
    {
        _ = processPath;
        return ReleaseFlavor.SelfContained;
    }

    private static InstallationMode InferInstallationMode(string installDirectory)
    {
        string normalizedDirectory = Path.GetFullPath(installDirectory);
        string programFiles = Path.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        string localAppData = Path.GetFullPath(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData,
                Environment.SpecialFolderOption.Create));

        return normalizedDirectory.StartsWith(programFiles, StringComparison.OrdinalIgnoreCase)
            ? InstallationMode.ProgramFiles
            : normalizedDirectory.StartsWith(localAppData, StringComparison.OrdinalIgnoreCase)
            ? InstallationMode.LocalAppData
            : InstallationMode.Unknown;
    }

    private static void RegisterElevatedUpdateTask(string taskName, string helperScriptPath)
    {
        string shellPath = ResolveShellExecutablePath();
        string currentUserId = WindowsIdentity.GetCurrent().Name
            ?? throw new UnexpectedStateException(CurrentWindowsIdentityDescription);
        string registrationScript = BuildElevatedTaskRegistrationScript(taskName, helperScriptPath, shellPath, currentUserId);
        string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(registrationScript));

        using Process process = Process.Start(new ProcessStartInfo
        {
            FileName = shellPath,
            Arguments = string.Join(
                CommandArgumentSeparator,
                NoLogoArgument,
                NoProfileArgument,
                NonInteractiveArgument,
                ExecutionPolicyArgument,
                BypassExecutionPolicy,
                EncodedCommandArgument,
                encodedCommand),
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden
        }) ?? throw new UnexpectedStateException(StartRegistrationProcessDescription);

        process.WaitForExit();
        if (process.ExitCode != SuccessExitCode)
        {
            throw new UnexpectedStateException(RegistrationProcessExitDescription);
        }
    }

    private static bool IsCurrentProcessElevated()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    internal static string ResolveShellExecutablePath()
    {
        string powerShellSevenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            ProgramFilesDirectoryName,
            PowerShellSevenDirectoryName,
            PowerShellSevenExecutableName);

        if (File.Exists(powerShellSevenPath))
        {
            return powerShellSevenPath;
        }

        string windowsPowerShellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            WindowsPowerShellRelativePath);

        return File.Exists(windowsPowerShellPath) ? windowsPowerShellPath : throw new FileNotFoundException(PowerShellResolutionDescription);
    }

    private static string SerializeReleaseFlavor(ReleaseFlavor releaseFlavor)
    {
        return releaseFlavor switch
        {
            ReleaseFlavor.SelfContained => SelfContainedFlavorName,
            ReleaseFlavor.FrameworkDependent => SelfContainedFlavorName,
            ReleaseFlavor.Unknown => SelfContainedFlavorName,
            _ => SelfContainedFlavorName
        };
    }

    private static string SerializeInstallationMode(InstallationMode installationMode)
    {
        return installationMode switch
        {
            InstallationMode.LocalAppData => LocalAppDataInstallationModeName,
            InstallationMode.ProgramFiles => ProgramFilesInstallationModeName,
            InstallationMode.Unknown => UnknownInstallationModeName,
            _ => UnknownInstallationModeName
        };
    }

    private static string ToPowerShellLiteral(string value)
    {
        return $"{SingleQuote}{value.Replace(SingleQuote, SingleQuote + SingleQuote, StringComparison.Ordinal)}{SingleQuote}";
    }

    private static PersistedInstallationMetadata? LoadPersistedInstallationMetadata(string manifestPath)
    {
        using FileStream stream = new(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize(
            stream,
            UpdateJsonContext.Default.PersistedInstallationMetadata);
    }

    private static string ResolveInstalledExecutableName(string currentExecutableName, InstallationMode installationMode)
    {
        return installationMode == InstallationMode.LocalAppData
            ? AppIdentity.ExecutableFileName
            : currentExecutableName;
    }

    private static void WritePersistedInstallationMetadata(
        string manifestPath,
        PersistedInstallationMetadata metadata)
    {
        using FileStream stream = new(manifestPath, FileMode.Create, FileAccess.Write, FileShare.None);
        JsonSerializer.Serialize(stream, metadata, UpdateJsonContext.Default.PersistedInstallationMetadata);
        stream.Flush(flushToDisk: true);
    }

    private static string BuildHelperScript(string requestPath)
    {
        return HelperScriptTemplate
            .Replace(HelperScriptRequestPathToken, ToPowerShellLiteral(requestPath), StringComparison.Ordinal)
            .Replace(LauncherScriptStartupValueNameToken, AppIdentity.StartupValueName, StringComparison.Ordinal)
            .Replace(LauncherScriptLegacyStartupValueNameToken, AppIdentity.LegacyStartupValueName, StringComparison.Ordinal);
    }

    private static string BuildLauncherScript()
    {
        return LauncherScriptTemplate;
    }
}
