using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetForceModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Name, ForceUnit Unit, string[] Aliases)[] Definitions =
    [
        ("N", "Newton", ForceUnit.Newton, ["newton", "newtons"]),
        ("kN", "Kilonewton", ForceUnit.Kilonewton, ["kilonewton", "kilonewtons"]),
        ("MN", "Meganewton", ForceUnit.Meganewton, ["meganewton", "meganewtons"]),
        ("mN", "Millinewton", ForceUnit.Millinewton, ["millinewton", "millinewtons"]),
        ("µN", "Micronewton", ForceUnit.Micronewton, ["micronewton", "micronewtons", "uN", "μN"]),
        ("lbf", "Pound-force", ForceUnit.PoundForce, ["pound-force", "pounds-force", "pound force", "pounds force"]),
        ("kgf", "Kilogram-force", ForceUnit.KilogramForce, ["kilogram-force", "kilograms-force", "kilogram force", "kilograms force"]),
        ("dyn", "Dyne", ForceUnit.Dyn, ["dyne", "dynes"])
    ];

    private static readonly string[] Targets = ["N", "kN", "lbf", "kgf", "mN"];
    private static readonly Dictionary<string, ForceUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);

    public UnitsNetForceModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Id,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = Targets.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Force";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.Force;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = Force.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}