namespace SolarEngine.Infrastructure.Localization;

internal static class AppLanguageCodes
{
    public const string English = "en";
    public const string Spanish = "es";
    public const string Default = English;

    public static string Normalize(string? languageCode)
    {
        return string.Equals(languageCode, Spanish, StringComparison.OrdinalIgnoreCase)
            ? Spanish
            : English;
    }
}
