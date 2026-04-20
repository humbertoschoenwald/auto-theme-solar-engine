using System.Globalization;
using SolarEngine.Infrastructure.Localization;
using Xunit;

namespace SolarEngine.Tests.Infrastructure.Localization;

/// <summary>
/// Verifies localization resource selection and fallback behavior.
/// </summary>
[Trait("TestLane", "Light")]
public sealed class JsonLocalizationCatalogTests
{
    /// <summary>
    /// Verifies Spanish UI culture selects Spanish resource text.
    /// </summary>
    [Fact]
    public void Indexer_ReturnsSpanishText_WhenCurrentUiCultureIsSpanish()
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");

        try
        {
            JsonLocalizationCatalog catalog = new();

            Assert.Equal("Configuración", catalog["settings.header"]);
            Assert.Equal("Español", catalog["settings.language.option.spanish"]);
            Assert.Equal("La última revisión falló. Se reintentará automáticamente.", catalog["settings.update.check_failed"]);
            Assert.Equal("Salir", catalog["tray.exit"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies unsupported UI cultures fall back to English resource text.
    /// </summary>
    [Fact]
    public void Indexer_FallsBackToEnglish_WhenCurrentUiCultureIsUnsupported()
    {
        CultureInfo originalCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");

        try
        {
            JsonLocalizationCatalog catalog = new();

            Assert.Equal("Settings", catalog["settings.header"]);
            Assert.Equal("English", catalog["settings.language.option.english"]);
            Assert.Equal("The last update check failed. Retrying automatically.", catalog["settings.update.check_failed"]);
            Assert.Equal("Exit", catalog["tray.exit"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }
}
