using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Themes.Infrastructure;

internal sealed partial class WindowsRegistryThemeMutator(StructuredLogPublisher logPublisher) : IThemeMutator
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppKeyPath = @"Software\AutoThemeSolarEngine";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";
    private const string SystemUsesLightThemeValueName = "SystemUsesLightTheme";
    private const string ColorPrevalenceValueName = "ColorPrevalence";
    private const string StoredDarkModeColorPrevalenceValueName = "StoredDarkModeColorPrevalence";
    private const string LightModeTaskbarPreferenceManagedValueName = "LightModeTaskbarPreferenceManaged";
    private const string UxThemeLibraryName = "uxtheme.dll";
    private const string RefreshImmersiveColorPolicyStateExport = "#104";
    private const string FlushMenuThemesExport = "#136";

    private static readonly nint HwndBroadcast = new(0xFFFF);

    private const string ShellTrayWindowClassName = "Shell_TrayWnd";
    private const string ShellSecondaryTrayWindowClassName = "Shell_SecondaryTrayWnd";
    private const string ProgramManagerWindowClassName = "Program";
    private const string WorkerWindowClassName = "WorkerW";

    private const int WmSysColorChange = 0x0015;
    private const int WmSettingChange = 0x001A;
    private const int WmThemeChanged = 0x031A;
    private const int WmDwmColorizationColorChanged = 0x0320;
    private const int SmtoAbortIfHung = 0x0002;
    private const uint RdwInvalidate = 0x0001;
    private const uint RdwAllChildren = 0x0080;
    private const uint RdwFrame = 0x0400;
    private const uint RdwUpdatenow = 0x0100;

    public Result<ThemeMode> Apply(ThemeMode mode)
    {
        try
        {
            int lightValue = mode == ThemeMode.Light ? 1 : 0;

            using RegistryKey personalizeKey = Registry.CurrentUser.CreateSubKey(PersonalizeKeyPath, writable: true)
                ?? throw new UnexpectedStateException("Resolve the Personalize registry key before mutating shell theme state.");

            personalizeKey.SetValue(AppsUseLightThemeValueName, lightValue, RegistryValueKind.DWord);
            personalizeKey.SetValue(SystemUsesLightThemeValueName, lightValue, RegistryValueKind.DWord);
            ApplyTaskbarColorPreference(mode, personalizeKey);
            personalizeKey.Flush();

            RefreshShellThemeState();

            logPublisher.Write($"Theme mutation committed: {mode}.");
            return Result<ThemeMode>.Success(mode);
        }
        catch (Exception exception)
        {
            logPublisher.Write($"Theme mutation failed: {exception.Message}");
            return Result<ThemeMode>.Failure(new Error("themes.mutator.registry_failure", "Isolate registry mutation failures behind a deterministic application contract."));
        }
    }

    public ThemeMode? TryGetCurrentMode()
    {
        using RegistryKey? personalizeKey = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath, writable: false);
        if (personalizeKey is null)
        {
            return null;
        }

        object? appsValue = personalizeKey.GetValue(AppsUseLightThemeValueName);
        object? systemValue = personalizeKey.GetValue(SystemUsesLightThemeValueName);

        return !TryReadDword(appsValue, out int appsLight) || !TryReadDword(systemValue, out int systemLight)
            ? null
            : appsLight != systemLight
            ? null
            : appsLight switch
            {
                1 => ThemeMode.Light,
                0 => ThemeMode.Dark,
                _ => null
            };
    }

    private static void ApplyTaskbarColorPreference(ThemeMode mode, RegistryKey personalizeKey)
    {
        if (mode == ThemeMode.Light)
        {
            StoreDarkModeTaskbarPreferenceIfNeeded(personalizeKey);
            personalizeKey.SetValue(ColorPrevalenceValueName, 0, RegistryValueKind.DWord);
            MarkTaskbarPreferenceAsManaged();
            return;
        }

        RestoreDarkModeTaskbarPreferenceIfNeeded(personalizeKey);
    }

    private static bool TryReadDword(object? value, out int result)
    {
        switch (value)
        {
            case int intValue:
                result = intValue;
                return true;

            case long longValue when longValue is >= int.MinValue and <= int.MaxValue:
                result = (int)longValue;
                return true;

            default:
                result = default;
                return false;
        }
    }

    private static void StoreDarkModeTaskbarPreferenceIfNeeded(RegistryKey personalizeKey)
    {
        object? currentColorPrevalenceValue = personalizeKey.GetValue(ColorPrevalenceValueName);
        if (!TryReadDword(currentColorPrevalenceValue, out int currentColorPrevalence))
        {
            return;
        }

        if (IsTaskbarPreferenceCurrentlyManagedByApplication() && currentColorPrevalence == 0)
        {
            return;
        }

        using RegistryKey appKey = Registry.CurrentUser.CreateSubKey(AppKeyPath, writable: true)
            ?? throw new UnexpectedStateException("Resolve the application registry key before persisting taskbar appearance preferences.");

        appKey.SetValue(
            StoredDarkModeColorPrevalenceValueName,
            currentColorPrevalence,
            RegistryValueKind.DWord);
    }

    private static void RestoreDarkModeTaskbarPreferenceIfNeeded(RegistryKey personalizeKey)
    {
        using RegistryKey appKey = Registry.CurrentUser.CreateSubKey(AppKeyPath, writable: true)
            ?? throw new UnexpectedStateException("Resolve the application registry key before restoring taskbar appearance preferences.");

        bool isManagedByApplication =
            TryReadDword(appKey.GetValue(LightModeTaskbarPreferenceManagedValueName), out int managedValue)
            && managedValue == 1;

        if (!isManagedByApplication)
        {
            return;
        }

        if (TryReadDword(appKey.GetValue(StoredDarkModeColorPrevalenceValueName), out int storedColorPrevalence))
        {
            personalizeKey.SetValue(ColorPrevalenceValueName, storedColorPrevalence, RegistryValueKind.DWord);
        }

        appKey.DeleteValue(LightModeTaskbarPreferenceManagedValueName, throwOnMissingValue: false);
    }

    private static void MarkTaskbarPreferenceAsManaged()
    {
        using RegistryKey appKey = Registry.CurrentUser.CreateSubKey(AppKeyPath, writable: true)
            ?? throw new UnexpectedStateException("Resolve the application registry key before tracking taskbar appearance ownership.");

        appKey.SetValue(LightModeTaskbarPreferenceManagedValueName, 1, RegistryValueKind.DWord);
    }

    private static bool IsTaskbarPreferenceCurrentlyManagedByApplication()
    {
        using RegistryKey? appKey = Registry.CurrentUser.OpenSubKey(AppKeyPath, writable: false);
        return appKey is not null && TryReadDword(appKey.GetValue(LightModeTaskbarPreferenceManagedValueName), out int managedValue)
            && managedValue == 1;
    }

    private static void RefreshShellThemeState()
    {
        BroadcastThemeChange("ImmersiveColorSet");
        BroadcastThemeChange("WindowsThemeElement");
        BroadcastThemeChange("SystemPalette");
        BroadcastThemeChange(null);

        if (TryRefreshImmersiveColorPolicyState())
        {
            TryFlushMenuThemes();
        }

        _ = TryUpdatePerUserSystemParameters();

        BroadcastWindowMessage(WmThemeChanged);
        BroadcastWindowMessage(WmDwmColorizationColorChanged);
        BroadcastWindowMessage(WmSysColorChange);

        RefreshShellWindowByClassName(ShellTrayWindowClassName);
        RefreshShellWindowsByClassName(ShellSecondaryTrayWindowClassName);
        RefreshShellWindowByClassName(ProgramManagerWindowClassName);
        RefreshShellWindowsByClassName(WorkerWindowClassName);
    }

    private static void BroadcastThemeChange(string? parameter)
    {
        _ = SendMessageTimeoutString(HwndBroadcast, WmSettingChange, nint.Zero, parameter, SmtoAbortIfHung, 2000, out _);
    }

    private static void BroadcastWindowMessage(int message)
    {
        _ = SendMessageTimeoutRaw(HwndBroadcast, message, nint.Zero, nint.Zero, SmtoAbortIfHung, 2000, out _);
    }

    private static void RefreshShellWindowByClassName(string className)
    {
        nint windowHandle = FindWindow(className, null);
        if (windowHandle == nint.Zero)
        {
            return;
        }

        RefreshShellWindow(windowHandle);
    }

    private static void RefreshShellWindowsByClassName(string className)
    {
        nint previousWindowHandle = nint.Zero;
        while (true)
        {
            nint windowHandle = FindWindowEx(nint.Zero, previousWindowHandle, className, null);
            if (windowHandle == nint.Zero)
            {
                return;
            }

            RefreshShellWindow(windowHandle);
            previousWindowHandle = windowHandle;
        }
    }

    private static void RefreshShellWindow(nint windowHandle)
    {
        BroadcastThemeChangeToWindow(windowHandle, "ImmersiveColorSet");
        BroadcastThemeChangeToWindow(windowHandle, "WindowsThemeElement");
        BroadcastThemeChangeToWindow(windowHandle, "SystemPalette");
        BroadcastThemeChangeToWindow(windowHandle, null);

        _ = SendMessageTimeoutRaw(windowHandle, WmThemeChanged, nint.Zero, nint.Zero, SmtoAbortIfHung, 2000, out _);
        _ = SendMessageTimeoutRaw(windowHandle, WmDwmColorizationColorChanged, nint.Zero, nint.Zero, SmtoAbortIfHung, 2000, out _);
        _ = SendMessageTimeoutRaw(windowHandle, WmSysColorChange, nint.Zero, nint.Zero, SmtoAbortIfHung, 2000, out _);
        _ = RedrawWindow(windowHandle, nint.Zero, nint.Zero, RdwInvalidate | RdwAllChildren | RdwFrame | RdwUpdatenow);
    }

    private static void BroadcastThemeChangeToWindow(nint windowHandle, string? parameter)
    {
        _ = SendMessageTimeoutString(windowHandle, WmSettingChange, nint.Zero, parameter, SmtoAbortIfHung, 2000, out _);
    }

    private static bool TryUpdatePerUserSystemParameters()
    {
        try
        {
            return UpdatePerUserSystemParameters(1, true);
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static bool TryRefreshImmersiveColorPolicyState()
    {
        if (!TryLoadUxThemeExport(RefreshImmersiveColorPolicyStateExport, out RefreshImmersiveColorPolicyStateDelegate? refresh))
        {
            return false;
        }

        refresh();
        return true;
    }

    private static void TryFlushMenuThemes()
    {
        if (!TryLoadUxThemeExport(FlushMenuThemesExport, out FlushMenuThemesDelegate? flush))
        {
            return;
        }

        flush();
    }

    private static bool TryLoadUxThemeExport<TDelegate>(string exportName, [NotNullWhen(true)] out TDelegate? exportDelegate)
        where TDelegate : Delegate
    {
        exportDelegate = null;

        if (!NativeLibrary.TryLoad(UxThemeLibraryName, out nint moduleHandle))
        {
            return false;
        }

        try
        {
            if (!NativeLibrary.TryGetExport(moduleHandle, exportName, out nint exportAddress))
            {
                return false;
            }

            exportDelegate = Marshal.GetDelegateForFunctionPointer<TDelegate>(exportAddress);
            return true;
        }
        finally
        {
            NativeLibrary.Free(moduleHandle);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void RefreshImmersiveColorPolicyStateDelegate();

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void FlushMenuThemesDelegate();

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint FindWindow(string? lpClassName, string? lpWindowName);

    [LibraryImport("user32.dll", EntryPoint = "FindWindowExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint FindWindowEx(nint hWndParent, nint hWndChildAfter, string? lpszClass, string? lpszWindow);

    [LibraryImport("user32.dll", EntryPoint = "RedrawWindow", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RedrawWindow(nint hWnd, nint lprcUpdate, nint hrgnUpdate, uint flags);

    [LibraryImport("user32.dll", EntryPoint = "UpdatePerUserSystemParameters", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UpdatePerUserSystemParameters(uint uiAction, [MarshalAs(UnmanagedType.Bool)] bool fWinIni);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial nint SendMessageTimeoutString(
        nint hWnd,
        int msg,
        nint wParam,
        string? lParam,
        int fuFlags,
        int uTimeout,
        out nint lpdwResult);

    [LibraryImport("user32.dll", EntryPoint = "SendMessageTimeoutW", SetLastError = true)]
    private static partial nint SendMessageTimeoutRaw(
        nint hWnd,
        int msg,
        nint wParam,
        nint lParam,
        int fuFlags,
        int uTimeout,
        out nint lpdwResult);

    [LibraryImport("user32.dll", EntryPoint = "SendNotifyMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SendNotifyMessage(nint hWnd, int msg, nint wParam, nint lParam);
}
