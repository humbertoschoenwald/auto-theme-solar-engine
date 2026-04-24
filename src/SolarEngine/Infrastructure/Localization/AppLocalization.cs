// Copyright (c) 2026 Humberto Schoenwald.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace SolarEngine.Infrastructure.Localization;

internal sealed class AppLocalization
{
    private JsonLocalizationCatalog _catalog = new(AppLanguageCodes.Default);

    public string LanguageCode { get; private set; } = AppLanguageCodes.Default;

    public string this[string key] => _catalog[key];

    public string Format(string key, params object?[] arguments)
    {
        return _catalog.Format(key, arguments);
    }

    public void UpdateLanguage(string? languageCode)
    {
        string normalizedLanguageCode = AppLanguageCodes.Normalize(languageCode);
        if (string.Equals(LanguageCode, normalizedLanguageCode, StringComparison.Ordinal))
        {
            return;
        }

        LanguageCode = normalizedLanguageCode;
        _catalog = new JsonLocalizationCatalog(normalizedLanguageCode);
    }
}
