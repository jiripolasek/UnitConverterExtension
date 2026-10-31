using System.Collections.Concurrent;
using System.Resources;

namespace JPSoftworks.UnitConverter.Core;

public static class ConverterText
{
    private static readonly ResourceManager Resources = new("JPSoftworks.UnitConverter.Core.Resources.Strings", typeof(ConverterText).Assembly);
    private static readonly ConcurrentDictionary<string, LocalizedUnit[]> UnitNames = new();
    private static readonly ConcurrentDictionary<(string Culture, ConversionWordLanguage Mode), string[]> ConversionWords = new();
    internal const ConversionWordLanguage DefaultConversionWordLanguage = ConversionWordLanguage.InterfaceInputAndEnglish;

    public static string Get(string text, CultureInfo? culture = null) =>
        Resources.GetString(text, GetResourceCulture(culture ?? CultureInfo.CurrentUICulture)) ?? text;

    public static string Format(string text, CultureInfo culture, params object[] arguments) =>
        string.Format(culture, Get(text, culture), arguments);

    public static IReadOnlyList<string> GetUnitNames(string category, string unit, CultureInfo? culture = null) =>
        GetUnits(culture).FirstOrDefault(entry => entry.Category == category && entry.Id == unit)?.Names ?? [];

    public static IReadOnlyList<string> GetConversionWords(CultureInfo culture, ConversionWordLanguage mode = ConversionWordLanguage.Default) =>
        ConversionWords.GetOrAdd((GetCultureKey(culture), mode == ConversionWordLanguage.Default ? DefaultConversionWordLanguage : mode), key =>
        {
            var words = GetQueryLanguages(culture).SelectMany(language => Get("ConversionConnectorWords", language)
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            return key.Mode is ConversionWordLanguage.InputLanguageAndEnglish or ConversionWordLanguage.InterfaceInputAndEnglish
                ? words.Concat(Get("ConversionConnectorWords", CultureInfo.InvariantCulture).Split('|')).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                : words.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        });

    internal static string GetCultureKey(CultureInfo culture) => culture is ConversionQueryCulture query ? query.CacheKey : culture.Name;

    internal static IEnumerable<CultureInfo> GetQueryLanguages(CultureInfo culture) =>
        culture is ConversionQueryCulture query ? query.Languages : [culture];

    internal static bool HasAmbiguousUnitName(string input, CultureInfo culture) => culture is ConversionQueryCulture && GetUnits(culture)
        .Where(entry => entry.Names.Contains(input, StringComparer.OrdinalIgnoreCase))
        .Select(entry => (entry.Category, entry.Id)).Distinct().Skip(1).Any();

    internal static string GetCjkQueryLanguage(CultureInfo culture)
    {
        var words = GetConversionWords(culture);
        return words.Contains("を") ? "ja" : words.Contains("를") ? "ko" :
            words.Contains("转") || words.Contains("轉") ? "zh" : string.Empty;
    }

    internal static string NormalizeUnit(string category, string input, CultureInfo culture)
    {
        var units = GetUnits(culture);
        var trimmed = input.Trim();
        // Localized names must not replace exact, case-sensitive unit IDs.
        return units.Any(entry => entry.Id == trimmed) ? trimmed :
            units.FirstOrDefault(entry => entry.Category == category && entry.Names.Contains(trimmed, StringComparer.OrdinalIgnoreCase))?.Id ?? input;
    }

    internal static UnitMetadata? Localize(string category, string unit, UnitMetadata? metadata, CultureInfo? culture)
    {
        if (metadata == null)
            return null;

        var names = GetUnitNames(category, unit, culture);
        return metadata with
        {
            DisplayName = names.Count > 0 ? names[0] : metadata.DisplayName,
            Description = Get(metadata.Description, culture),
            System = Get(metadata.System, culture)
        };
    }

    private static LocalizedUnit[] GetUnits(CultureInfo? culture)
    {
        culture ??= CultureInfo.CurrentUICulture;
        return UnitNames.GetOrAdd(GetCultureKey(culture), _ => culture is ConversionQueryCulture query
            ? query.Languages.SelectMany(GetUnits).GroupBy(entry => (entry.Category, entry.Id))
                .Select(group => new LocalizedUnit(group.Key.Category, group.Key.Id,
                    group.SelectMany(entry => entry.Names).Distinct(StringComparer.OrdinalIgnoreCase).ToArray())).ToArray()
            : (Resources.GetString("UnitNames", GetResourceCulture(culture)) ?? string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split('|'))
                .Select(parts => new LocalizedUnit(parts[0], parts[1], parts[2..]))
                .ToArray());
    }

    private static CultureInfo GetResourceCulture(CultureInfo culture)
    {
        var name = culture.TwoLetterISOLanguageName switch
        {
            "cs" => "cs-CZ",
            "de" => "de-DE",
            "es" => "es-ES",
            "fr" => "fr",
            "it" => "it-IT",
            "ja" => "ja-JP",
            "ko" => "ko-KR",
            "nl" => "nl-NL",
            "pl" => "pl-PL",
            "pt" => culture.Name.Split('-').Contains("BR", StringComparer.OrdinalIgnoreCase) ? "pt-BR" : "pt-PT",
            "zh" => culture.Name.Split('-').Contains("Hans", StringComparer.OrdinalIgnoreCase) ? "zh-CN" :
                culture.Name.Split('-').Any(part => part.Equals("Hant", StringComparison.OrdinalIgnoreCase) ||
                    part is "TW" or "HK" or "MO") ? "zh-TW" : "zh-CN",
            _ => null
        };
        return name == null ? culture : CultureInfo.GetCultureInfo(name);
    }

    private sealed record LocalizedUnit(string Category, string Id, string[] Names);
}