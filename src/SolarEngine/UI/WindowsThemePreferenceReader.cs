// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Win32;

namespace SolarEngine.UI;

internal static class WindowsThemePreferenceReader
{
    private const int DisabledThemeValue = 0;
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public static bool ShouldUseDarkWindowFrame()
    {
        using RegistryKey? personalizeKey = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath, writable: false);
        object? appsValue = personalizeKey?.GetValue(AppsUseLightThemeValueName);

        return appsValue switch
        {
            int value => value == DisabledThemeValue,
            byte value => value == DisabledThemeValue,
            _ => false
        };
    }
}
