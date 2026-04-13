using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace SolarEngine.Infrastructure.Localization;

internal sealed class JsonLocalizationCatalog
{
    private const string DefaultLanguageCode = "en";
    private const string SpanishLanguageCode = "es";
    private const string ResourceNamespace = "SolarEngine.Resources.Localization";

    private readonly FrozenDictionary<string, string> _defaultTranslations;
    private readonly FrozenDictionary<string, string> _activeTranslations;

    public JsonLocalizationCatalog()
    {
        string languageCode = ResolvePreferredLanguageCode();
        _defaultTranslations = LoadTranslations(DefaultLanguageCode);
        _activeTranslations = string.Equals(languageCode, DefaultLanguageCode, StringComparison.Ordinal)
            ? _defaultTranslations
            : LoadTranslations(languageCode);
    }

    public string this[string key]
        => _activeTranslations.TryGetValue(key, out string? value)
            ? value
            : _defaultTranslations.TryGetValue(key, out string? fallback)
                ? fallback
                : key;

    public string Format(string key, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, this[key], arguments);
    }

    private static string ResolvePreferredLanguageCode()
    {
        string twoLetterCode = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return string.Equals(twoLetterCode, SpanishLanguageCode, StringComparison.OrdinalIgnoreCase)
            ? SpanishLanguageCode
            : DefaultLanguageCode;
    }

    private static FrozenDictionary<string, string> LoadTranslations(string languageCode)
    {
        Assembly assembly = typeof(JsonLocalizationCatalog).Assembly;
        string resourceName = $"{ResourceNamespace}.{languageCode}.json";

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded localization resource was not found: {resourceName}");
        using JsonDocument document = JsonDocument.Parse(stream);

        Dictionary<string, string> translations = [with(StringComparer.Ordinal)];
        foreach (JsonProperty property in document.RootElement.EnumerateObject())
        {
            translations[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return translations.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
