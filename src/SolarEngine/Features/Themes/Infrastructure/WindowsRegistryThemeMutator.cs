// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;
using SolarEngine.Features.Themes.Domain;
using SolarEngine.Infrastructure.Logging;
using SolarEngine.Shared.Core;

namespace SolarEngine.Features.Themes.Infrastructure;

internal sealed partial class WindowsRegistryThemeMutator(StructuredLogPublisher logPublisher) : IThemeMutator
{
    private const string ThemesKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes";
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppKeyPath = @"Software\AutoThemeSolarEngine";
    private const string CurrentThemeValueName = "CurrentTheme";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";
    private const string SystemUsesLightThemeValueName = "SystemUsesLightTheme";
    private const string ColorPrevalenceValueName = "ColorPrevalence";
    private const string EnableTransparencyValueName = "EnableTransparency";
    private const string StoredDarkModeColorPrevalenceValueName = "StoredDarkModeColorPrevalence";
    private const string LightModeTaskbarPreferenceManagedValueName = "LightModeTaskbarPreferenceManaged";
    private const string UxThemeLibraryName = "uxtheme.dll";
    private const string RefreshImmersiveColorPolicyStateExport = "#104";
    private const string FlushMenuThemesExport = "#136";
    private const string RegistryKeyResolutionDescription = "Resolve the Personalize registry key before mutating shell theme state.";
    private const string RegistryFailureCode = "themes.mutator.registry_failure";
    private const string RegistryFailureDescription = "Isolate registry mutation failures behind a deterministic application contract.";
    private const string ThemeMetadataResolutionDescription = "Resolve the Themes registry key before synchronizing shell theme metadata.";
    private const string PersistTaskbarPreferenceDescription = "Resolve the application registry key before persisting taskbar appearance preferences.";
    private const string RestoreTaskbarPreferenceDescription = "Resolve the application registry key before restoring taskbar appearance preferences.";
    private const string ManageTaskbarPreferenceDescription = "Resolve the application registry key before tracking taskbar appearance ownership.";
    private const string ImmersiveColorSetParameter = "ImmersiveColorSet";
    private const string WindowsThemeElementParameter = "WindowsThemeElement";
    private const string SystemPaletteParameter = "SystemPalette";
    private const int ThemeRequestTimeoutMilliseconds = 2000;
    private const int TopLevelWindowNotificationTimeoutMilliseconds = 500;
    private const int DarkThemeValue = 0;
    private const int LightThemeValue = 1;
    private const int ManagedPreferenceEnabledValue = 1;
    private const int TransparencyDisabledValue = 0;
    private const int TransparencyEnabledValue = 1;
    private const uint UpdatePerUserSystemParametersAction = 1;

    private static readonly nint s_hwndBroadcast = new(0xFFFF);

    private const int WmSysColorChange = 0x0015;
    private const int WmSettingChange = 0x001A;
    private const int WmThemeChanged = 0x031A;
    private const int WmDwmColorizationColorChanged = 0x0320;
    private const int WindowClassNameBufferLength = 256;
    private const int WindowClassNameStartIndex = 0;
    private const int EmptyWindowClassNameLength = 0;
    private const int SmtoAbortIfHung = 0x0002;
    private const uint RdwInvalidate = 0x0001;
    private const uint RdwAllChildren = 0x0080;
    private const uint RdwFrame = 0x0400;
    private const uint RdwUpdatenow = 0x0100;

    public Result<ThemeMode> Apply(ThemeMode mode)
    {
        try
        {
            int lightValue = mode == ThemeMode.Light ? LightThemeValue : DarkThemeValue;

            using RegistryKey personalizeKey = Registry.CurrentUser.CreateSubKey(PersonalizeKeyPath, writable: true)
                ?? throw new UnexpectedStateException(RegistryKeyResolutionDescription);

            personalizeKey.SetValue(AppsUseLightThemeValueName, lightValue, RegistryValueKind.DWord);
            personalizeKey.SetValue(SystemUsesLightThemeValueName, lightValue, RegistryValueKind.DWord);
            ApplyTaskbarColorPreference(mode, personalizeKey);
            PulseTransparencyPreferenceIfNeeded(personalizeKey);
            personalizeKey.Flush();

            SynchronizeCurrentThemeMetadata(mode);
            RefreshShellThemeState();

            logPublisher.Write($"Theme mutation committed: {mode}.");
            return Result<ThemeMode>.Success(mode);
        }
        catch (Exception exception) when (
            exception is IOException
            or SecurityException
            or UnauthorizedAccessException
            or Win32Exception
            or ExternalException
            or UnexpectedStateException)
        {
            logPublisher.Write($"Theme mutation failed: {exception.Message}");
            return Result<ThemeMode>.Failure(new Error(RegistryFailureCode, RegistryFailureDescription));
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

        return ResolveThemeMode(appsValue, systemValue);
    }

    internal static ThemeMode? ResolveThemeMode(object? appsValue, object? systemValue)
    {
        bool hasAppsThemeValue = TryReadDword(appsValue, out int appsLight);
        bool hasSystemThemeValue = TryReadDword(systemValue, out int systemLight);

        return (hasAppsThemeValue, hasSystemThemeValue) switch
        {
            (true, true) when appsLight != systemLight => null,
            (true, _) => ResolveThemeModeValue(appsLight),
            (false, true) => ResolveThemeModeValue(systemLight),
            _ => null
        };
    }

    private static ThemeMode? ResolveThemeModeValue(int lightValue)
    {
        return lightValue switch
        {
            LightThemeValue => ThemeMode.Light,
            DarkThemeValue => ThemeMode.Dark,
            _ => null
        };
    }

    private static void PulseTransparencyPreferenceIfNeeded(RegistryKey personalizeKey)
    {
        if (!TryReadDword(personalizeKey.GetValue(EnableTransparencyValueName), out int originalTransparencyValue))
        {
            return;
        }

        int pulseTransparencyValue = originalTransparencyValue == TransparencyDisabledValue
            ? TransparencyEnabledValue
            : TransparencyDisabledValue;

        personalizeKey.SetValue(EnableTransparencyValueName, pulseTransparencyValue, RegistryValueKind.DWord);
        personalizeKey.SetValue(EnableTransparencyValueName, originalTransparencyValue, RegistryValueKind.DWord);
    }

    private static void SynchronizeCurrentThemeMetadata(ThemeMode mode)
    {
        using RegistryKey themesKey = Registry.CurrentUser.CreateSubKey(ThemesKeyPath, writable: true)
            ?? throw new UnexpectedStateException(ThemeMetadataResolutionDescription);

        string? currentThemePath = themesKey.GetValue(CurrentThemeValueName) as string;
        string synchronizedThemePath = ThemeMetadataSynchronizer.Synchronize(mode, currentThemePath);
        themesKey.SetValue(CurrentThemeValueName, synchronizedThemePath, RegistryValueKind.String);
        themesKey.Flush();
    }

    private static void ApplyTaskbarColorPreference(ThemeMode mode, RegistryKey personalizeKey)
    {
        if (mode == ThemeMode.Light)
        {
            StoreDarkModeTaskbarPreferenceIfNeeded(personalizeKey);
            personalizeKey.SetValue(ColorPrevalenceValueName, DarkThemeValue, RegistryValueKind.DWord);
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

        if (IsTaskbarPreferenceCurrentlyManagedByApplication() && currentColorPrevalence == DarkThemeValue)
        {
            return;
        }

        using RegistryKey appKey = Registry.CurrentUser.CreateSubKey(AppKeyPath, writable: true)
            ?? throw new UnexpectedStateException(PersistTaskbarPreferenceDescription);

        appKey.SetValue(
            StoredDarkModeColorPrevalenceValueName,
            currentColorPrevalence,
            RegistryValueKind.DWord);
    }

    private static void RestoreDarkModeTaskbarPreferenceIfNeeded(RegistryKey personalizeKey)
    {
        using RegistryKey appKey = Registry.CurrentUser.CreateSubKey(AppKeyPath, writable: true)
            ?? throw new UnexpectedStateException(RestoreTaskbarPreferenceDescription);

        bool isManagedByApplication =
            TryReadDword(appKey.GetValue(LightModeTaskbarPreferenceManagedValueName), out int managedValue)
            && managedValue == ManagedPreferenceEnabledValue;

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
            ?? throw new UnexpectedStateException(ManageTaskbarPreferenceDescription);

        appKey.SetValue(LightModeTaskbarPreferenceManagedValueName, ManagedPreferenceEnabledValue, RegistryValueKind.DWord);
    }

    private static bool IsTaskbarPreferenceCurrentlyManagedByApplication()
    {
        using RegistryKey? appKey = Registry.CurrentUser.OpenSubKey(AppKeyPath, writable: false);
        return appKey is not null && TryReadDword(appKey.GetValue(LightModeTaskbarPreferenceManagedValueName), out int managedValue)
            && managedValue == ManagedPreferenceEnabledValue;
    }

    private static void RefreshShellThemeState()
    {
        BroadcastThemeChange(ImmersiveColorSetParameter);
        BroadcastThemeChange(WindowsThemeElementParameter);
        BroadcastThemeChange(SystemPaletteParameter);
        BroadcastThemeChange(null);

        if (TryRefreshImmersiveColorPolicyState())
        {
            TryFlushMenuThemes();
        }

        _ = TryUpdatePerUserSystemParameters();

        BroadcastWindowMessage(WmThemeChanged);
        BroadcastWindowMessage(WmDwmColorizationColorChanged);
        BroadcastWindowMessage(WmSysColorChange);

        List<ShellWindowInfo> topLevelWindows = EnumerateTopLevelWindows();

        ShellThemeRefreshPlanner.NotifyTopLevelWindows(
            topLevelWindows,
            NotifyThemeChangeWindow);

        ShellThemeRefreshPlanner.RefreshShellWindows(
            topLevelWindows,
            EnumerateDescendantWindowHandles,
            RefreshShellWindow);
    }

    private static void BroadcastThemeChange(string? parameter)
    {
        _ = SendMessageTimeoutString(s_hwndBroadcast, WmSettingChange, nint.Zero, parameter, SmtoAbortIfHung, ThemeRequestTimeoutMilliseconds, out _);
    }

    private static void BroadcastWindowMessage(int message)
    {
        _ = SendMessageTimeoutRaw(s_hwndBroadcast, message, nint.Zero, nint.Zero, SmtoAbortIfHung, ThemeRequestTimeoutMilliseconds, out _);
    }

    private static List<ShellWindowInfo> EnumerateTopLevelWindows()
    {
        List<ShellWindowInfo> windows = [];

        _ = EnumWindows(
            (windowHandle, _) =>
            {
                string className = GetWindowClassName(windowHandle);
                if (!string.IsNullOrEmpty(className))
                {
                    windows.Add(new ShellWindowInfo(windowHandle, className));
                }

                return true;
            },
            nint.Zero);

        return windows;
    }

    private static List<nint> EnumerateDescendantWindowHandles(nint windowHandle)
    {
        List<nint> descendantWindowHandles = [];

        _ = EnumChildWindows(
            windowHandle,
            (descendantWindowHandle, _) =>
            {
                descendantWindowHandles.Add(descendantWindowHandle);
                return true;
            },
            nint.Zero);

        return descendantWindowHandles;
    }

    private static string GetWindowClassName(nint windowHandle)
    {
        char[] buffer = new char[WindowClassNameBufferLength];
        int length = GetClassName(windowHandle, buffer, buffer.Length);
        return length <= EmptyWindowClassNameLength
            ? string.Empty
            : new string(buffer, WindowClassNameStartIndex, length);
    }

    private static void RefreshShellWindow(nint windowHandle)
    {
        NotifyThemeChangeWindow(windowHandle, ThemeRequestTimeoutMilliseconds);
        _ = RedrawWindow(windowHandle, nint.Zero, nint.Zero, RdwInvalidate | RdwAllChildren | RdwFrame | RdwUpdatenow);
    }

    private static void NotifyThemeChangeWindow(nint windowHandle)
    {
        NotifyThemeChangeWindow(windowHandle, TopLevelWindowNotificationTimeoutMilliseconds);
    }

    private static void NotifyThemeChangeWindow(nint windowHandle, int timeoutMilliseconds)
    {
        BroadcastThemeChangeToWindow(windowHandle, ImmersiveColorSetParameter, timeoutMilliseconds);
        BroadcastThemeChangeToWindow(windowHandle, WindowsThemeElementParameter, timeoutMilliseconds);
        BroadcastThemeChangeToWindow(windowHandle, SystemPaletteParameter, timeoutMilliseconds);
        BroadcastThemeChangeToWindow(windowHandle, null, timeoutMilliseconds);

        _ = SendMessageTimeoutRaw(windowHandle, WmThemeChanged, nint.Zero, nint.Zero, SmtoAbortIfHung, timeoutMilliseconds, out _);
        _ = SendMessageTimeoutRaw(windowHandle, WmDwmColorizationColorChanged, nint.Zero, nint.Zero, SmtoAbortIfHung, timeoutMilliseconds, out _);
        _ = SendMessageTimeoutRaw(windowHandle, WmSysColorChange, nint.Zero, nint.Zero, SmtoAbortIfHung, timeoutMilliseconds, out _);
    }

    private static void BroadcastThemeChangeToWindow(nint windowHandle, string? parameter, int timeoutMilliseconds)
    {
        _ = SendMessageTimeoutString(windowHandle, WmSettingChange, nint.Zero, parameter, SmtoAbortIfHung, timeoutMilliseconds, out _);
    }

    private static bool TryUpdatePerUserSystemParameters()
    {
        try
        {
            return UpdatePerUserSystemParameters(UpdatePerUserSystemParametersAction, true);
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

    private delegate bool EnumWindowsProcedure(nint hWnd, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "EnumWindows", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(EnumWindowsProcedure lpEnumFunc, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "EnumChildWindows", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumChildWindows(nint hWndParent, EnumWindowsProcedure lpEnumFunc, nint lParam);

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true)]
    private static partial int GetClassName(nint hWnd, [Out] char[] lpClassName, int nMaxCount);

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
