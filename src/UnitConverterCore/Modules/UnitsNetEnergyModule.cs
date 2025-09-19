// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Energy conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetEnergyModule : DescriptorBasedConversionModule
{
    private sealed class EnergyAmbiguityProvider : IAmbiguityProvider
    {
        // Ambiguous tokens commonly used without context
        private static readonly Dictionary<string, string[]> _ambiguous = new(StringComparer.OrdinalIgnoreCase)
        {
            // Nutrition vs physics/chemistry
            ["calorie"] = ["kcal", "cal"],
            ["calories"] = ["kcal", "cal"],
            ["Calorie"] = ["kcal", "cal"],
            ["Calories"] = ["kcal", "cal"]
        };

        public bool TryGetAmbiguity(string token, out string[] candidates, out string reasonMessage)
        {
            if (_ambiguous.TryGetValue(token.Trim(), out candidates!))
            {
                reasonMessage = "Ambiguous unit token. It can mean kilocalorie (kcal) or calorie (cal).";
                return true;
            }

            candidates = [];
            reasonMessage = string.Empty;
            return false;
        }

        public IEnumerable<string> GetAmbiguousTokens() => _ambiguous.Keys;
    }

    public UnitsNetEnergyModule() : base(CreateUnitDescriptors()) { }

    protected override IAmbiguityProvider? AmbiguityProvider => new EnergyAmbiguityProvider();

    public override string Category => "Energy";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Energy;

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

            var energy = CreateEnergy(value, from);
            var converted = ConvertToUnit(energy, to);
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
                Id = "eV",
                DisplayName = "Electron Volt",
                Symbol = "eV",
                System = "Physics",
                IsCommon = false,
                Aliases = ["electron volt", "electron volts"],
                CommonTargets = ["J", "kJ"],
                SuggestionPriority = 90,
                Tags = ["physics", "atomic", "particle"],
                Description = "Elementary charge times one volt"
            },
            new UnitDescriptor
            {
                Id = "J",
                DisplayName = "Joule",
                Symbol = "J",
                System = "Metric",
                IsCommon = true,
                Aliases = ["joule", "joules"],
                CommonTargets = ["kJ", "cal", "kcal", "BTU", "kWh"],
                SuggestionPriority = 10,
                Tags = ["SI", "metric"],
                Description = "SI unit of energy"
            },
            new UnitDescriptor
            {
                Id = "kJ",
                DisplayName = "Kilojoule",
                Symbol = "kJ",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilojoule", "kilojoules"],
                CommonTargets = ["J", "kWh", "BTU"],
                SuggestionPriority = 15,
                Tags = ["SI", "metric"],
                Description = "1000 joules"
            },
            new UnitDescriptor
            {
                Id = "cal",
                DisplayName = "Calorie",
                Symbol = "cal",
                System = "Metric",
                IsCommon = true,
                Aliases = [
                    "small calorie", "small calories",
                    "thermochemical calorie", "thermochemical calories",
                    "gram calorie", "gram calories",
                    "cal-th", "cal th"
                ],
                CommonTargets = ["J", "kcal"],
                SuggestionPriority = 25,
                Tags = ["metric", "nutrition", "chemistry"],
                Description = "Thermochemical calorie (4.184 J)"
            },
            new UnitDescriptor
            {
                Id = "kcal",
                DisplayName = "Kilocalorie",
                Symbol = "kcal",
                System = "Metric",
                IsCommon = true,
                Aliases = [
                    "kilocalorie", "kilocalories",
                    // Removed ambiguous and conflicting aliases: "Cal", "Calorie", "Calories"
                    "food calorie", "food calories",
                    "large calorie", "large calories"
                ],
                CommonTargets = ["kJ", "J", "BTU"],
                SuggestionPriority = 5,
                Tags = ["metric", "nutrition", "food"],
                Description = "1000 calories, commonly used for food energy"
            },
            new UnitDescriptor
            {
                Id = "ft·lb",
                DisplayName = "Foot-pound",
                Symbol = "ft·lb",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["ft-lb", "ft lb", "foot-pound", "foot-pounds", "lbf·ft", "lb-ft"],
                CommonTargets = ["J", "kJ"],
                SuggestionPriority = 40,
                Tags = ["imperial", "mechanical"],
                Description = "Imperial unit of energy"
            },
            new UnitDescriptor
            {
                Id = "BTU",
                DisplayName = "British Thermal Unit",
                Symbol = "BTU",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["btu", "british thermal unit", "british thermal units"],
                CommonTargets = ["kJ", "kWh", "J"],
                SuggestionPriority = 20,
                Tags = ["imperial", "thermal", "hvac"],
                Description = "Traditional unit of heat energy"
            },
            new UnitDescriptor
            {
                Id = "kWh",
                DisplayName = "Kilowatt-hour",
                Symbol = "kWh",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilowatt-hour", "kilowatt-hours", "kW·h", "kW h"],
                CommonTargets = ["kJ", "J", "BTU"],
                SuggestionPriority = 8,
                Tags = ["metric", "electrical", "utility"],
                Description = "Unit of electrical energy consumption"
            }
        ];
    }

    private static Energy CreateEnergy(double value, string unit) => unit switch
    {
        "eV" => Energy.FromElectronVolts(value),
        "J" => Energy.FromJoules(value),
        "kJ" => Energy.FromKilojoules(value),
        "cal" => Energy.FromCalories(value),
        "kcal" => Energy.FromKilocalories(value),
        "ft·lb" => Energy.FromFootPounds(value),
        "BTU" => Energy.FromBritishThermalUnits(value),
        "kWh" => Energy.FromKilowattHours(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Energy e, string unit) => unit switch
    {
        "eV" => e.ElectronVolts,
        "J" => e.Joules,
        "kJ" => e.Kilojoules,
        "cal" => e.Calories,
        "kcal" => e.Kilocalories,
        "ft·lb" => e.FootPounds,
        "BTU" => e.BritishThermalUnits,
        "kWh" => e.KilowattHours,
        _ => null
    };
}
