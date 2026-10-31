using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetTorqueModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Symbol, string Name, TorqueUnit Unit, string[] Aliases)[] Definitions =
    [
        ("Nm", "N·m", "Newton meter", TorqueUnit.NewtonMeter, ["N m", "N*m", "newton meter", "newton meters", "newton metre", "newton metres", "newton-meter", "newton-meters"]),
        ("kNm", "kN·m", "Kilonewton meter", TorqueUnit.KilonewtonMeter, ["kN m", "kN*m", "kilonewton meter", "kilonewton meters", "kilonewton metre", "kilonewton metres"]),
        ("Ncm", "N·cm", "Newton centimeter", TorqueUnit.NewtonCentimeter, ["N cm", "N*cm", "newton centimeter", "newton centimeters", "newton centimetre", "newton centimetres"]),
        ("Nmm", "N·mm", "Newton millimeter", TorqueUnit.NewtonMillimeter, ["N mm", "N*mm", "newton millimeter", "newton millimeters", "newton millimetre", "newton millimetres"]),
        ("lbf·ft", "lbf·ft", "Pound-force foot", TorqueUnit.PoundForceFoot, ["lbf ft", "lbf*ft", "lbf-ft", "pound-force foot", "pound-force feet"]),
        ("lbf·in", "lbf·in", "Pound-force inch", TorqueUnit.PoundForceInch, ["lbf in", "lbf*in", "lbf-in", "pound-force inch", "pound-force inches"]),
        ("kgf·m", "kgf·m", "Kilogram-force meter", TorqueUnit.KilogramForceMeter, ["kgf m", "kgf*m", "kgf-m", "kilogram-force meter", "kilogram-force meters", "kilogram-force metre", "kilogram-force metres"])
    ];

    private static readonly string[] Targets = ["Nm", "kNm", "lbf·ft", "lbf·in", "kgf·m"];
    private static readonly Dictionary<string, TorqueUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);

    public UnitsNetTorqueModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Symbol,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Torque";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.Torque;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = Torque.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}