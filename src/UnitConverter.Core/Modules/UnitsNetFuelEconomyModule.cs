using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetFuelEconomyModule : DescriptorBasedConversionModule
{
    private static readonly Dictionary<string, FuelEfficiencyUnit> UnitMap = new(StringComparer.Ordinal)
    {
        ["L/100km"] = FuelEfficiencyUnit.LiterPer100Kilometers,
        ["km/L"] = FuelEfficiencyUnit.KilometerPerLiter,
        ["mpg US"] = FuelEfficiencyUnit.MilePerUsGallon,
        ["mpg UK"] = FuelEfficiencyUnit.MilePerUkGallon
    };

    private static readonly IAmbiguityProvider MpgAmbiguity = new FuelAmbiguityProvider();

    public UnitsNetFuelEconomyModule() : base(CreateDescriptors()) { }

    public override string Category => "FuelEconomy";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    protected override UnitType GetUnitType() => UnitType.FuelEconomy;
    protected override IAmbiguityProvider AmbiguityProvider => MpgAmbiguity;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) || value <= 0 ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = FuelEfficiency.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result) && result > 0;
        }
        catch (ArgumentException) { return false; }
    }

    private static IEnumerable<UnitDescriptor> CreateDescriptors()
    {
        UnitDescriptor[] descriptors =
        [
            new()
            {
                Id = "L/100km", DisplayName = "Liter per 100 kilometers",
                Aliases = ["L/100 km", "liter per 100 kilometers", "liters per 100 kilometers", "litre per 100 kilometres", "litres per 100 kilometres"]
            },
            new()
            {
                Id = "km/L", DisplayName = "Kilometer per liter",
                Aliases = ["kilometer per liter", "kilometers per liter", "kilometre per litre", "kilometres per litre"]
            },
            new()
            {
                Id = "mpg US", DisplayName = "Mile per US gallon",
                Aliases = ["US mpg", "mpg (US)", "mile per US gallon", "miles per US gallon"]
            },
            new()
            {
                Id = "mpg UK", DisplayName = "Mile per imperial gallon",
                Aliases = ["UK mpg", "mpg (UK)", "imperial mpg", "mpg imperial", "mile per UK gallon", "miles per UK gallon", "mile per imperial gallon", "miles per imperial gallon"]
            }
        ];
        return descriptors.Select(unit => unit with
        {
            Symbol = unit.Id,
            CommonTargets = UnitMap.Keys.Where(target => target != unit.Id).ToArray()
        });
    }

    private sealed class FuelAmbiguityProvider : IAmbiguityProvider
    {
        public IEnumerable<string> GetAmbiguousTokens() => ["mpg", "mile per gallon", "miles per gallon"];

        public bool TryGetAmbiguity(string token, out string[] candidates, out string reasonMessage)
        {
            if (GetAmbiguousTokens().Contains(token.Trim(), StringComparer.OrdinalIgnoreCase))
            {
                candidates = ["mpg US", "mpg UK"];
                reasonMessage = "Miles per gallon requires a US or UK gallon.";
                return true;
            }
            candidates = [];
            reasonMessage = string.Empty;
            return false;
        }
    }
}