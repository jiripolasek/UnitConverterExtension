using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetAccelerationModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Symbol, string Name, AccelerationUnit Unit, string[] Aliases)[] Definitions =
    [
        ("m/s²", "m/s²", "Meter per second squared", AccelerationUnit.MeterPerSecondSquared, ["m/s2", "m/s^2", "meter per second squared", "meters per second squared", "metre per second squared", "metres per second squared"]),
        ("cm/s²", "cm/s²", "Centimeter per second squared", AccelerationUnit.CentimeterPerSecondSquared, ["cm/s2", "cm/s^2", "centimeter per second squared", "centimeters per second squared", "centimetre per second squared", "centimetres per second squared"]),
        ("mm/s²", "mm/s²", "Millimeter per second squared", AccelerationUnit.MillimeterPerSecondSquared, ["mm/s2", "mm/s^2", "millimeter per second squared", "millimeters per second squared", "millimetre per second squared", "millimetres per second squared"]),
        ("ft/s²", "ft/s²", "Foot per second squared", AccelerationUnit.FootPerSecondSquared, ["ft/s2", "ft/s^2", "foot per second squared", "feet per second squared"]),
        ("in/s²", "in/s²", "Inch per second squared", AccelerationUnit.InchPerSecondSquared, ["in/s2", "in/s^2", "inch per second squared", "inches per second squared"]),
        ("g0", "g₀", "Standard gravity", AccelerationUnit.StandardGravity, ["standard gravity", "standard gravities"])
    ];

    private static readonly string[] Targets = ["m/s²", "ft/s²", "g0", "cm/s²", "mm/s²"];
    private static readonly Dictionary<string, AccelerationUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);

    public UnitsNetAccelerationModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Symbol,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Acceleration";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    protected override UnitType GetUnitType() => UnitType.Acceleration;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = Acceleration.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}