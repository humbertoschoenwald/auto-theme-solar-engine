// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Win32;

namespace SolarEngine.UI;

internal static class WindowsThemePreferenceReader
{
    private const int DisabledThemeValue = 0;
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";
    private const string SystemUsesLightThemeValueName = "SystemUsesLightTheme";

    public static bool ShouldUseDarkWindowFrame()
    {
        using RegistryKey? personalizeKey = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath, writable: false);
        object? appsValue = personalizeKey?.GetValue(AppsUseLightThemeValueName);
        object? systemValue = personalizeKey?.GetValue(SystemUsesLightThemeValueName);

        return ShouldUseDarkWindowFrame(appsValue, systemValue);
    }

    internal static bool ShouldUseDarkWindowFrame(object? appsValue, object? systemValue)
    {
        bool hasAppsThemeValue = TryReadLightThemeValue(appsValue, out int appsLight);
        bool hasSystemThemeValue = TryReadLightThemeValue(systemValue, out int systemLight);

        return (hasAppsThemeValue, hasSystemThemeValue) switch
        {
            (true, _) => appsLight == DisabledThemeValue,
            (false, true) => systemLight == DisabledThemeValue,
            _ => false
        };
    }

    private static bool TryReadLightThemeValue(object? value, out int lightThemeValue)
    {
        switch (value)
        {
            case int intValue:
                lightThemeValue = intValue;
                return true;

            case byte byteValue:
                lightThemeValue = byteValue;
                return true;

            default:
                lightThemeValue = default;
                return false;
        }
    }
}
