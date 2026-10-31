using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetCurrentModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Name, ElectricCurrentUnit Unit, string[] Aliases)[] Definitions =
    [
        ("A", "Ampere", ElectricCurrentUnit.Ampere, ["ampere", "amperes", "amp", "amps"]),
        ("mA", "Milliampere", ElectricCurrentUnit.Milliampere, ["milliampere", "milliamperes", "milliamp", "milliamps"]),
        ("µA", "Microampere", ElectricCurrentUnit.Microampere, ["microampere", "microamperes", "microamp", "microamps", "uA", "μA"]),
        ("kA", "Kiloampere", ElectricCurrentUnit.Kiloampere, ["kiloampere", "kiloamperes", "kiloamp", "kiloamps"]),
        ("MA", "Megaampere", ElectricCurrentUnit.Megaampere, ["megaampere", "megaamperes", "megaamp", "megaamps"])
    ];

    private static readonly string[] Targets = ["A", "mA", "µA", "kA", "MA"];
    private static readonly Dictionary<string, ElectricCurrentUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);

    public UnitsNetCurrentModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Id,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Current";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.Current;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = ElectricCurrent.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}