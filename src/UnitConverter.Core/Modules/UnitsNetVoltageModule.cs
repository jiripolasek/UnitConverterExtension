using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetVoltageModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Name, ElectricPotentialUnit Unit, string[] Aliases)[] Definitions =
    [
        ("V", "Volt", ElectricPotentialUnit.Volt, ["volt", "volts", "v"]),
        ("mV", "Millivolt", ElectricPotentialUnit.Millivolt, ["millivolt", "millivolts"]),
        ("µV", "Microvolt", ElectricPotentialUnit.Microvolt, ["microvolt", "microvolts", "uV", "μV"]),
        ("kV", "Kilovolt", ElectricPotentialUnit.Kilovolt, ["kilovolt", "kilovolts"]),
        ("MV", "Megavolt", ElectricPotentialUnit.Megavolt, ["megavolt", "megavolts"])
    ];

    private static readonly string[] Targets = ["V", "mV", "µV", "kV", "MV"];
    private static readonly Dictionary<string, ElectricPotentialUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);

    public UnitsNetVoltageModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Id,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Voltage";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.Voltage;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = ElectricPotential.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}