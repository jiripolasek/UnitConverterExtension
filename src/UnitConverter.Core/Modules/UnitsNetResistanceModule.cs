using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetResistanceModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Symbol, string Name, ElectricResistanceUnit Unit, string[] Aliases)[] Definitions =
    [
        ("ohm", "Ω", "Ohm", ElectricResistanceUnit.Ohm, ["ohm", "ohms"]),
        ("mohm", "mΩ", "Milliohm", ElectricResistanceUnit.Milliohm, ["milliohm", "milliohms"]),
        ("uohm", "µΩ", "Microohm", ElectricResistanceUnit.Microohm, ["microohm", "microohms", "uΩ", "μΩ"]),
        ("kohm", "kΩ", "Kiloohm", ElectricResistanceUnit.Kiloohm, ["kiloohm", "kiloohms", "kilohm", "kilohms"]),
        ("Mohm", "MΩ", "Megaohm", ElectricResistanceUnit.Megaohm, ["megaohm", "megaohms", "megohm", "megohms"])
    ];

    private static readonly string[] Targets = ["ohm", "mohm", "uohm", "kohm", "Mohm"];
    private static readonly Dictionary<string, ElectricResistanceUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);

    public UnitsNetResistanceModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Symbol,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Resistance";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.Resistance;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = ElectricResistance.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}