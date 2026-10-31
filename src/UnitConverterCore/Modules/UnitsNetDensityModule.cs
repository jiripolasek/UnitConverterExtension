using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetDensityModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Name, DensityUnit Unit, string[] Aliases)[] Definitions =
    [
        ("kg/m³", "Kilogram per cubic meter", DensityUnit.KilogramPerCubicMeter, ["kg/m3", "kg/m^3", "kilogram per cubic meter", "kilograms per cubic meter", "kilogram per cubic metre", "kilograms per cubic metre"]),
        ("g/cm³", "Gram per cubic centimeter", DensityUnit.GramPerCubicCentimeter, ["g/cm3", "g/cm^3", "gram per cubic centimeter", "grams per cubic centimeter", "gram per cubic centimetre", "grams per cubic centimetre"]),
        ("g/mL", "Gram per milliliter", DensityUnit.GramPerMilliliter, ["gram per milliliter", "grams per milliliter", "gram per millilitre", "grams per millilitre"]),
        ("kg/L", "Kilogram per liter", DensityUnit.KilogramPerLiter, ["kilogram per liter", "kilograms per liter", "kilogram per litre", "kilograms per litre"]),
        ("lb/ft³", "Pound per cubic foot", DensityUnit.PoundPerCubicFoot, ["lb/ft3", "lb/ft^3", "pound per cubic foot", "pounds per cubic foot"]),
        ("lb/in³", "Pound per cubic inch", DensityUnit.PoundPerCubicInch, ["lb/in3", "lb/in^3", "pound per cubic inch", "pounds per cubic inch"])
    ];

    private static readonly string[] Targets = ["kg/m³", "g/cm³", "g/mL", "kg/L", "lb/ft³"];
    private static readonly Dictionary<string, double> Factors = Definitions.ToDictionary(unit => unit.Id, unit => KilogramsPerCubicMeter(unit.Unit), StringComparer.Ordinal);

    public UnitsNetDensityModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Id,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Density";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    protected override UnitType GetUnitType() => UnitType.Density;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        result = value * (Factors[from] / Factors[to]);
        return double.IsFinite(result);
    }

    // UnitsNet 5.75 rounds its imperial density factors. Derive these two from its mass and length
    // definitions until the density factors retain the same precision (1 lb/in³ = 1728 lb/ft³).
    private static double KilogramsPerCubicMeter(DensityUnit unit) => unit switch
    {
        DensityUnit.PoundPerCubicFoot => Mass.FromPounds(1).Kilograms / Math.Pow(Length.FromFeet(1).Meters, 3),
        DensityUnit.PoundPerCubicInch => Mass.FromPounds(1).Kilograms / Math.Pow(Length.FromInches(1).Meters, 3),
        _ => Density.From(1, unit).KilogramsPerCubicMeter
    };
}