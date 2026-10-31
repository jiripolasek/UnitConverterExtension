// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Mass/Weight conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetMassModule : DescriptorBasedConversionModule
{
    public UnitsNetMassModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Mass";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Mass;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            if (!TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
                !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            {
                return false;
            }

            var mass = CreateMass(value, from);
            var converted = ConvertToUnit(mass, to);
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
                Id = "ct",
                DisplayName = "Carat",
                Symbol = "ct",
                System = "Metric",
                IsCommon = false,
                Aliases = ["carat", "carats"],
                CommonTargets = ["g", "mg"],
                SuggestionPriority = 70,
                Tags = ["metric", "precious", "jewelry"],
                Description = "Unit for measuring precious stones and pearls (200 mg)"
            },
            new UnitDescriptor
            {
                Id = "mg",
                DisplayName = "Milligram",
                Symbol = "mg",
                System = "Metric",
                IsCommon = true,
                Aliases = ["milligram", "milligrams"],
                CommonTargets = ["g", "cg", "ct"],
                SuggestionPriority = 30,
                Tags = ["SI", "metric", "small", "medicine"],
                Description = "One thousandth of a gram"
            },
            new UnitDescriptor
            {
                Id = "cg",
                DisplayName = "Centigram",
                Symbol = "cg",
                System = "Metric",
                IsCommon = false,
                Aliases = ["centigram", "centigrams"],
                CommonTargets = ["mg", "dg", "g"],
                SuggestionPriority = 80,
                Tags = ["SI", "metric"],
                Description = "One hundredth of a gram"
            },
            new UnitDescriptor
            {
                Id = "dg",
                DisplayName = "Decigram",
                Symbol = "dg",
                System = "Metric",
                IsCommon = false,
                Aliases = ["decigram", "decigrams"],
                CommonTargets = ["cg", "g"],
                SuggestionPriority = 85,
                Tags = ["SI", "metric"],
                Description = "One tenth of a gram"
            },
            new UnitDescriptor
            {
                Id = "g",
                DisplayName = "Gram",
                Symbol = "g",
                System = "Metric",
                IsCommon = true,
                Aliases = ["gram", "grams"],
                CommonTargets = ["kg", "oz", "mg", "dag"],
                SuggestionPriority = 10,
                Tags = ["SI", "metric", "common"],
                Description = "Base unit of mass in CGS system"
            },
            new UnitDescriptor
            {
                Id = "dag",
                DisplayName = "Dekagram",
                Symbol = "dag",
                System = "Metric",
                IsCommon = false,
                Aliases = ["dekagram", "dekagrams", "decagram", "decagrams"],
                CommonTargets = ["g", "hg"],
                SuggestionPriority = 75,
                Tags = ["SI", "metric"],
                Description = "Ten grams"
            },
            new UnitDescriptor
            {
                Id = "hg",
                DisplayName = "Hectogram",
                Symbol = "hg",
                System = "Metric",
                IsCommon = false,
                Aliases = ["hectogram", "hectograms"],
                CommonTargets = ["dag", "kg", "g"],
                SuggestionPriority = 65,
                Tags = ["SI", "metric"],
                Description = "One hundred grams"
            },
            new UnitDescriptor
            {
                Id = "kg",
                DisplayName = "Kilogram",
                Symbol = "kg",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilogram", "kilograms"],
                CommonTargets = ["g", "lb", "t"],
                SuggestionPriority = 5,
                Tags = ["SI", "base", "metric", "common"],
                Description = "SI base unit of mass"
            },
            new UnitDescriptor
            {
                Id = "t",
                DisplayName = "Metric Tonne",
                Symbol = "t",
                System = "Metric",
                IsCommon = true,
                Aliases = ["tonne", "tonnes", "metric ton", "metric tons"],
                CommonTargets = ["kg", "short tn"],
                SuggestionPriority = 40,
                Tags = ["SI", "metric", "large"],
                Description = "1000 kilograms"
            },
            new UnitDescriptor
            {
                Id = "oz",
                DisplayName = "Ounce",
                Symbol = "oz",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["ounce", "ounces"],
                CommonTargets = ["g", "lb"],
                SuggestionPriority = 15,
                Tags = ["imperial", "common", "US"],
                Description = "1/16 of a pound"
            },
            new UnitDescriptor
            {
                Id = "lb",
                DisplayName = "Pound",
                Symbol = "lb",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["pound", "pounds", "lbs"],
                CommonTargets = ["kg", "oz", "stone"],
                SuggestionPriority = 8,
                Tags = ["imperial", "common", "US"],
                Description = "16 ounces"
            },
            new UnitDescriptor
            {
                Id = "stone",
                DisplayName = "Stone",
                Symbol = "st",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["stone", "stones"],
                CommonTargets = ["kg", "lb"],
                SuggestionPriority = 60,
                Tags = ["imperial", "UK", "body-weight"],
                Description = "14 pounds (UK/Ireland)"
            },
            new UnitDescriptor
            {
                Id = "short tn",
                DisplayName = "Short Ton",
                Symbol = "short tn",
                System = "US",
                IsCommon = true,
                Aliases = ["short ton", "short tons", "US ton", "US tons", "ton", "tons"],
                CommonTargets = ["t", "kg", "lb"],
                SuggestionPriority = 45,
                Tags = ["US", "imperial", "large"],
                Description = "2000 pounds (US)"
            }
        ];
    }

    private static Mass CreateMass(double value, string unit) => unit switch
    {
        "ct" => Mass.FromGrams(value * 0.2), // 1 carat = 0.2 grams
        "mg" => Mass.FromMilligrams(value),
        "cg" => Mass.FromCentigrams(value),
        "dg" => Mass.FromDecigrams(value),
        "g" => Mass.FromGrams(value),
        "dag" => Mass.FromDecagrams(value),
        "hg" => Mass.FromHectograms(value),
        "kg" => Mass.FromKilograms(value),
        "t" => Mass.FromTonnes(value),
        "oz" => Mass.FromOunces(value),
        "lb" => Mass.FromPounds(value),
        "stone" => Mass.FromStone(value),
        "short tn" => Mass.FromShortTons(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Mass mass, string unit) => unit switch
    {
        "ct" => mass.Grams / 0.2, // 1 carat = 0.2 grams
        "mg" => mass.Milligrams,
        "cg" => mass.Centigrams,
        "dg" => mass.Decigrams,
        "g" => mass.Grams,
        "dag" => mass.Decagrams,
        "hg" => mass.Hectograms,
        "kg" => mass.Kilograms,
        "t" => mass.Tonnes,
        "oz" => mass.Ounces,
        "lb" => mass.Pounds,
        "stone" => mass.Stone,
        "short tn" => mass.ShortTons,
        _ => null
    };
}