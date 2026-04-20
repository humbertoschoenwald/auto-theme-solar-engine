using SolarEngine.Infrastructure.Localization;
using Xunit;

namespace SolarEngine.Tests.Infrastructure.Localization;

/// <summary>
/// Verifies runtime localization state changes switch resources deterministically.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class AppLocalizationTests
{
    /// <summary>
    /// Verifies switching to Spanish changes the exposed UI strings.
    /// </summary>
    [Fact]
    public void UpdateLanguage_SwitchesCatalogToSpanish()
    {
        AppLocalization localization = new();

        localization.UpdateLanguage(AppLanguageCodes.Spanish);

        Assert.Equal(AppLanguageCodes.Spanish, localization.LanguageCode);
        Assert.Equal("Inicio", localization["settings.tab.home"]);
    }

    /// <summary>
    /// Verifies unsupported language codes fall back to English.
    /// </summary>
    [Fact]
    public void UpdateLanguage_FallsBackToEnglishForUnsupportedCodes()
    {
        AppLocalization localization = new();
        localization.UpdateLanguage(AppLanguageCodes.Spanish);

        localization.UpdateLanguage("fr");

        Assert.Equal(AppLanguageCodes.English, localization.LanguageCode);
        Assert.Equal("Settings", localization["settings.header"]);
    }

    /// <summary>
    /// Verifies formatted strings flow through the active catalog.
    /// </summary>
    [Fact]
    public void Format_UsesActiveLanguageTemplate()
    {
        AppLocalization localization = new();
        localization.UpdateLanguage(AppLanguageCodes.Spanish);

        string text = localization.Format("status.schedule_window", "06:30", "19:45");

        Assert.Equal("Amanecer 06:30 | Atardecer 19:45", text);
    }
}
