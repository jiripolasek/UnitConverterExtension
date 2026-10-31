// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Volume conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetVolumeModule : DescriptorBasedConversionModule
{
    private readonly IUnitPreferenceProvider _preferences;

    public UnitsNetVolumeModule(IUnitPreferenceProvider? preferences = null) : base(CreateUnitDescriptors())
    {
        this._preferences = preferences ?? new DefaultUnitPreferenceProvider();
    }

    public override string Category => "Volume";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Volume;
    
    private static readonly HashSet<string> BritishCultureNames =
    [
        "en-AI", "en-VG", "en-GB", "en-KY", "en-MS", "en-AG", "en-DM", 
        "en-GD", "en-KN", "en-LC", "en-VC", "en-IE", "en-GY", "en-AE",
        "en-IM", "en-JE", "en-GG", "en-FK", "en-GI", "en-SH"
    ];

    public override bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        normalizedUnit = string.Empty;
        if (string.IsNullOrWhiteSpace(unit))
            return false;

        var u = ConverterText.NormalizeUnit(Category, unit.Trim(), culture);
        var uLower = u.ToLowerInvariant();

        // Fast explicit case-insensitive mapping for known ids/symbols/phrases
        if (TryMapExplicitCaseInsensitive(uLower, out normalizedUnit))
        {
            return true;
        }

        // Resolve preference (Auto resolved by culture)
        var pref = this._preferences.VolumePreference;
        if (pref == VolumeSystemPreference.Auto)
        {
            try
            {
                var name = (culture ?? CultureInfo.CurrentCulture).Name ?? string.Empty;
                pref = BritishCultureNames.Contains(name)
                    ? VolumeSystemPreference.Imperial
                    : VolumeSystemPreference.US;
            }
            catch
            {
                pref = VolumeSystemPreference.US;
            }
        }

        static bool IsWord(string text, string singular)
        {
            if (string.Equals(text, singular, StringComparison.OrdinalIgnoreCase))
                return true;
            if (text.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                var stem = text[..^1];
                return string.Equals(stem, singular, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        // Ambiguous phrases/abbrevs that require preference
        var isFluidOuncePhrase = string.Equals(uLower, "fluid ounce", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(uLower, "fluid ounces", StringComparison.OrdinalIgnoreCase);
        var isAmbiguousAbbrev = uLower is "gal" or "qt" or "pt" or "fl oz" or "fl. oz" or "fl.oz";

        // Preference-based mapping for ambiguous words/abbreviations
        if (IsWord(u, "gallon") || IsWord(u, "quart") || IsWord(u, "pint") || isFluidOuncePhrase || isAmbiguousAbbrev)
        {
            if (pref == VolumeSystemPreference.Imperial)
            {
                normalizedUnit = isFluidOuncePhrase || uLower is "fl oz" or "fl. oz" or "fl.oz"
                    ? "imp fl oz"
                    : IsWord(u, "gallon") || uLower == "gal" ? "imp gal"
                    : IsWord(u, "quart") || uLower == "qt" ? "imp qt"
                    : /* pint */ "imp pt";
                return true;
            }

            // US
            normalizedUnit = isFluidOuncePhrase || uLower is "fl oz" or "fl. oz" or "fl.oz"
                ? "us fl oz"
                : IsWord(u, "gallon") || uLower == "gal" ? "us gal"
                : IsWord(u, "quart") || uLower == "qt" ? "us qt"
                : /* pint */ "us pt";
            return true;
        }

        // Otherwise, try standard descriptor normalization (for metric, cup, tsp, tbsp, and explicit imp/us ids)
        if (this.Units.TryNormalize(u, out normalizedUnit, out _))
        {
            return true;
        }

        // Final fallback: case-insensitive match on Id/Symbol/Alias within this module only
        var match = this.Units.GetAll().FirstOrDefault(d =>
            string.Equals(d.Id, u, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(d.Symbol) && string.Equals(d.Symbol, u, StringComparison.OrdinalIgnoreCase)) ||
            d.Aliases.Any(a => string.Equals(a, u, StringComparison.OrdinalIgnoreCase))
        );
        if (match != null)
        {
            normalizedUnit = match.Id;
            return true;
        }

        return false;
    }

    private static bool TryMapExplicitCaseInsensitive(string uLower, out string normalized)
    {
        normalized = string.Empty;
        // normalize punctuation variant for fl oz
        var normalizedLower = uLower.Replace("fl. oz", "fl oz").Replace("fl.oz", "fl oz");
        return (normalizedLower) switch
        {
            // US explicit ids and phrases
            "us fl oz" or "us fluid ounce" or "us fluid ounces" => Ret("us fl oz", out normalized),
            "us pt" or "us pint" or "us pints" => Ret("us pt", out normalized),
            "us qt" or "us quart" or "us quarts" => Ret("us qt", out normalized),
            "us gal" or "us gallon" or "us gallons" => Ret("us gal", out normalized),
            // Imperial explicit ids and phrases
            "imp fl oz" or "imperial fl oz" or "imperial fluid ounce" or "imperial fluid ounces" => Ret("imp fl oz", out normalized),
            "imp pt" or "imperial pt" or "imperial pint" or "imperial pints" => Ret("imp pt", out normalized),
            "imp qt" or "imperial qt" or "imperial quart" or "imperial quarts" => Ret("imp qt", out normalized),
            "imp gal" or "imperial gal" or "imperial gallon" or "imperial gallons" => Ret("imp gal", out normalized),
            // Non-ambiguous
            "ml" => Ret("ml", out normalized),
            "l" or "liter" or "litre" or "liters" or "litres" => Ret("l", out normalized),
            "tsp" or "teaspoon" or "teaspoons" => Ret("tsp", out normalized),
            "tbsp" or "tablespoon" or "tablespoons" => Ret("tbsp", out normalized),
            "cup" or "cups" => Ret("cup", out normalized),
            _ => false
        };

        static bool Ret(string v, out string o)
        {
            o = v;
            return true;
        }
    }

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            if (!this.TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
                !this.TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            {
                return false;
            }

            var volume = CreateVolume(value, from);
            var converted = ConvertToUnit(volume, to);
            if (converted.HasValue)
            {
                result = converted.Value;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static UnitDescriptor[] CreateUnitDescriptors()
    {
        return
        [
            new UnitDescriptor
            {
                Id = "ml",
                DisplayName = "Milliliter",
                Symbol = "mL",
                System = "Metric",
                IsCommon = true,
                Aliases = ["milliliter", "millilitre", "milliliters", "millilitres"],
                CommonTargets = ["l", "us fl oz", "cup"],
                SuggestionPriority = 15,
                Tags = ["SI", "metric", "common", "cooking"],
                Description = "One thousandth of a liter"
            },
            new UnitDescriptor
            {
                Id = "l",
                DisplayName = "Liter",
                Symbol = "L",
                System = "Metric",
                IsCommon = true,
                Aliases = ["liter", "litre", "liters", "litres"],
                CommonTargets = ["ml", "us gal", "us qt", "us pt"],
                SuggestionPriority = 5,
                Tags = ["SI", "metric", "common"],
                Description = "Basic metric unit of volume"
            },
            new UnitDescriptor
            {
                Id = "tsp",
                DisplayName = "Teaspoon",
                Symbol = "tsp",
                System = "US",
                IsCommon = true,
                Aliases = ["teaspoon", "teaspoons"],
                CommonTargets = ["ml", "tbsp"],
                SuggestionPriority = 25,
                Tags = ["US", "cooking", "small"],
                Description = "1/3 tablespoon (US)"
            },
            new UnitDescriptor
            {
                Id = "tbsp",
                DisplayName = "Tablespoon",
                Symbol = "tbsp",
                System = "US",
                IsCommon = true,
                Aliases = ["tablespoon", "tablespoons"],
                CommonTargets = ["ml", "us fl oz", "tsp"],
                SuggestionPriority = 20,
                Tags = ["US", "cooking"],
                Description = "3 teaspoons (US)"
            },
            // US explicit ids
            new UnitDescriptor
            {
                Id = "us fl oz",
                DisplayName = "US Fluid Ounce",
                Symbol = "US fl oz",
                System = "US",
                IsCommon = true,
                Aliases = ["us fluid ounce", "us fluid ounces", "us fl. oz", "us fl.oz", "us floz"],
                CommonTargets = ["ml", "cup", "tbsp"],
                SuggestionPriority = 18,
                Tags = ["US", "common"],
                Description = "1/8 US cup"
            },
            new UnitDescriptor
            {
                Id = "us pt",
                DisplayName = "US Pint",
                Symbol = "US pt",
                System = "US",
                IsCommon = true,
                Aliases = ["us pint", "us pints"],
                CommonTargets = ["l", "ml", "cup", "us qt"],
                SuggestionPriority = 30,
                Tags = ["US", "common"],
                Description = "2 US cups"
            },
            new UnitDescriptor
            {
                Id = "us qt",
                DisplayName = "US Quart",
                Symbol = "US qt",
                System = "US",
                IsCommon = true,
                Aliases = ["us quart", "us quarts"],
                CommonTargets = ["l", "us pt", "us gal"],
                SuggestionPriority = 35,
                Tags = ["US", "common"],
                Description = "2 US pints"
            },
            new UnitDescriptor
            {
                Id = "us gal",
                DisplayName = "US Gallon",
                Symbol = "US gal",
                System = "US",
                IsCommon = true,
                Aliases = ["us gallon", "us gallons"],
                CommonTargets = ["l", "us qt"],
                SuggestionPriority = 40,
                Tags = ["US", "common", "automotive"],
                Description = "4 US quarts"
            },
            // Non-ambiguous US unit
            new UnitDescriptor
            {
                Id = "cup",
                DisplayName = "Cup",
                Symbol = "cup",
                System = "US",
                IsCommon = true,
                Aliases = ["cups"],
                CommonTargets = ["ml", "l", "us fl oz", "us pt"],
                SuggestionPriority = 12,
                Tags = ["US", "cooking", "common"],
                Description = "8 US fluid ounces"
            },
            // Imperial explicit ids
            new UnitDescriptor
            {
                Id = "imp fl oz",
                DisplayName = "Imperial Fluid Ounce",
                Symbol = "imp fl oz",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["imperial fluid ounce", "imperial fluid ounces"],
                CommonTargets = ["ml", "us fl oz"],
                SuggestionPriority = 70,
                Tags = ["imperial", "UK"],
                Description = "1/20 imperial pint"
            },
            new UnitDescriptor
            {
                Id = "imp pt",
                DisplayName = "Imperial Pint",
                Symbol = "imp pt",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["imperial pint", "imperial pints"],
                CommonTargets = ["l", "us pt"],
                SuggestionPriority = 75,
                Tags = ["imperial", "UK"],
                Description = "20 imperial fluid ounces"
            },
            new UnitDescriptor
            {
                Id = "imp qt",
                DisplayName = "Imperial Quart",
                Symbol = "imp qt",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["imperial quart", "imperial quarts"],
                CommonTargets = ["l", "us qt"],
                SuggestionPriority = 80,
                Tags = ["imperial", "UK"],
                Description = "2 imperial pints"
            },
            new UnitDescriptor
            {
                Id = "imp gal",
                DisplayName = "Imperial Gallon",
                Symbol = "imp gal",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["imperial gallon", "imperial gallons"],
                CommonTargets = ["l", "us gal", "imp qt"],
                SuggestionPriority = 85,
                Tags = ["imperial", "UK"],
                Description = "4 imperial quarts"
            }
        ];
    }

    private static Volume CreateVolume(double value, string unit) => unit switch
    {
        // Metric
        "ml" => Volume.FromMilliliters(value),
        "l" => Volume.FromLiters(value),
        // US
        "us fl oz" => Volume.FromUsOunces(value),
        "tsp" => Volume.FromUsTeaspoons(value),
        "tbsp" => Volume.FromUsTablespoons(value),
        "cup" => Volume.FromUsOunces(value * 8), // 1 cup = 8 US fl oz
        "us pt" => Volume.FromUsPints(value),
        "us qt" => Volume.FromUsQuarts(value),
        "us gal" => Volume.FromUsGallons(value),
        // Imperial
        "imp fl oz" => Volume.FromImperialOunces(value),
        "imp pt" => Volume.FromImperialPints(value),
        "imp qt" => Volume.FromImperialQuarts(value),
        "imp gal" => Volume.FromImperialGallons(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Volume volume, string unit) => unit switch
    {
        // Metric
        "ml" => volume.Milliliters,
        "l" => volume.Liters,
        // US
        "us fl oz" => volume.UsOunces,
        "tsp" => volume.UsTeaspoons,
        "tbsp" => volume.UsTablespoons,
        "cup" => volume.UsOunces / 8, // 1 cup = 8 US fl oz
        "us pt" => volume.UsPints,
        "us qt" => volume.UsQuarts,
        "us gal" => volume.UsGallons,
        // Imperial
        "imp fl oz" => volume.ImperialOunces,
        "imp pt" => volume.ImperialPints,
        "imp qt" => volume.ImperialQuarts,
        "imp gal" => volume.ImperialGallons,
        _ => null
    };
}