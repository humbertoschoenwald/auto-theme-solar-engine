using Microsoft.Win32;

namespace SolarEngine.UI;

internal static class WindowsThemePreferenceReader
{
    private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string AppsUseLightThemeValueName = "AppsUseLightTheme";

    public static bool ShouldUseDarkWindowFrame()
    {
        using RegistryKey? personalizeKey = Registry.CurrentUser.OpenSubKey(PersonalizeKeyPath, writable: false);
        object? appsValue = personalizeKey?.GetValue(AppsUseLightThemeValueName);

        return appsValue switch
        {
            int value => value == 0,
            byte value => value == 0,
            _ => false
        };
    }
}
