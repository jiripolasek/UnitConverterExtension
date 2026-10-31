using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetFrequencyModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Name, FrequencyUnit Unit, string[] Aliases)[] Definitions =
    [
        ("Hz", "Hertz", FrequencyUnit.Hertz, ["hertz", "hz", "cycle per second", "cycles per second", "rps", "revolution per second", "revolutions per second", "1/s", "s^-1", "s⁻¹"]),
        ("kHz", "Kilohertz", FrequencyUnit.Kilohertz, ["kilohertz", "khz"]),
        ("MHz", "Megahertz", FrequencyUnit.Megahertz, ["megahertz"]),
        ("GHz", "Gigahertz", FrequencyUnit.Gigahertz, ["gigahertz", "ghz"]),
        ("THz", "Terahertz", FrequencyUnit.Terahertz, ["terahertz", "thz"]),
        ("mHz", "Millihertz", FrequencyUnit.Millihertz, ["millihertz"]),
        ("µHz", "Microhertz", FrequencyUnit.Microhertz, ["microhertz", "uHz", "μHz"]),
        ("rpm", "Revolution per minute", FrequencyUnit.CyclePerMinute, ["revolution per minute", "revolutions per minute", "cycle per minute", "cycles per minute", "cpm"]),
        ("bpm", "Beat per minute", FrequencyUnit.BeatPerMinute, ["beat per minute", "beats per minute"]),
        ("rad/s", "Radian per second", FrequencyUnit.RadianPerSecond, ["radian per second", "radians per second"])
    ];

    private static readonly Dictionary<string, FrequencyUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);
    private static readonly string[] CommonTargetIds = ["Hz", "kHz", "MHz", "GHz", "rpm"];

    public UnitsNetFrequencyModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Id,
        DisplayName = unit.Name,
        Aliases = unit.Aliases,
        CommonTargets = CommonTargetIds.Where(target => target != unit.Id).ToArray()
    })) { }

    public override string Category => "Frequency";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.Frequency;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = Frequency.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }
}