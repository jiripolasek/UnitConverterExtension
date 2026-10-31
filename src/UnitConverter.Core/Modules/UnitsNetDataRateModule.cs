using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetDataRateModule : DescriptorBasedConversionModule
{
    private static readonly (string Id, string Name, BitRateUnit Unit)[] Definitions =
    [
        ("b/s", "Bit per second", BitRateUnit.BitPerSecond),
        ("B/s", "Byte per second", BitRateUnit.BytePerSecond),
        ("kb/s", "Kilobit per second", BitRateUnit.KilobitPerSecond),
        ("kB/s", "Kilobyte per second", BitRateUnit.KilobytePerSecond),
        ("Mb/s", "Megabit per second", BitRateUnit.MegabitPerSecond),
        ("MB/s", "Megabyte per second", BitRateUnit.MegabytePerSecond),
        ("Gb/s", "Gigabit per second", BitRateUnit.GigabitPerSecond),
        ("GB/s", "Gigabyte per second", BitRateUnit.GigabytePerSecond),
        ("Tb/s", "Terabit per second", BitRateUnit.TerabitPerSecond),
        ("TB/s", "Terabyte per second", BitRateUnit.TerabytePerSecond),
        ("Kib/s", "Kibibit per second", BitRateUnit.KibibitPerSecond),
        ("KiB/s", "Kibibyte per second", BitRateUnit.KibibytePerSecond),
        ("Mib/s", "Mebibit per second", BitRateUnit.MebibitPerSecond),
        ("MiB/s", "Mebibyte per second", BitRateUnit.MebibytePerSecond),
        ("Gib/s", "Gibibit per second", BitRateUnit.GibibitPerSecond),
        ("GiB/s", "Gibibyte per second", BitRateUnit.GibibytePerSecond),
        ("Tib/s", "Tebibit per second", BitRateUnit.TebibitPerSecond),
        ("TiB/s", "Tebibyte per second", BitRateUnit.TebibytePerSecond)
    ];

    private static readonly Dictionary<string, BitRateUnit> UnitMap = Definitions.ToDictionary(unit => unit.Id, unit => unit.Unit, StringComparer.Ordinal);
    private static readonly string[] CommonTargetIds = ["Mb/s", "MB/s", "Mib/s", "MiB/s", "Gb/s"];
    private readonly IReadOnlyDictionary<string, string[]> _supportedUnits;

    public UnitsNetDataRateModule() : base(Definitions.Select(unit => new UnitDescriptor
    {
        Id = unit.Id,
        Symbol = unit.Id,
        DisplayName = unit.Name,
        Aliases = [unit.Name, unit.Name.Replace(" per", "s per", StringComparison.Ordinal)],
        System = "Digital",
        CommonTargets = CommonTargetIds.Where(target => target != unit.Id).ToArray()
    }))
    {
        _supportedUnits = base.GetSupportedUnits().ToDictionary(unit => unit.Key,
            unit => unit.Value.Append(unit.Key[..^2] + "ps").ToArray(), StringComparer.Ordinal).AsReadOnly();
    }

    public override string Category => "DataRate";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    public override bool IsCaseSensitive => true;
    protected override UnitType GetUnitType() => UnitType.DataRate;

    public override IReadOnlyDictionary<string, string[]> GetSupportedUnits() => _supportedUnits;

    public override bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        unit = unit.Trim();
        if (unit.EndsWith("ps", StringComparison.Ordinal))
        {
            unit = unit[..^2] + "/s";
        }

        return base.TryNormalizeUnit(unit, culture, out normalizedUnit);
    }

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = (double)BitRate.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
        catch (OverflowException) { return false; }
    }
}
