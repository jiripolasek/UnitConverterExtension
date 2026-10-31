// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Power conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetPowerModule : DescriptorBasedConversionModule
{
    public UnitsNetPowerModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Power";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Power;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            if (!TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
                !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            {
                return false;
            }

            var power = CreatePower(value, from);
            var converted = ConvertToUnit(power, to);
            if (converted.HasValue)
            {
                result = converted.Value;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static UnitDescriptor[] CreateUnitDescriptors()
    {
        return
        [
            new UnitDescriptor
            {
                Id = "W",
                DisplayName = "Watt",
                Symbol = "W",
                System = "Metric",
                IsCommon = true,
                Aliases = ["watt", "watts"],
                CommonTargets = ["kW", "hp"],
                SuggestionPriority = 10,
                Tags = ["SI", "metric", "electrical"],
                Description = "SI unit of power"
            },
            new UnitDescriptor
            {
                Id = "kW",
                DisplayName = "Kilowatt",
                Symbol = "kW",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilowatt", "kilowatts"],
                CommonTargets = ["W", "hp"],
                SuggestionPriority = 5,
                Tags = ["SI", "metric", "electrical", "automotive"],
                Description = "1000 watts"
            },
            new UnitDescriptor
            {
                Id = "hp",
                DisplayName = "Horsepower",
                Symbol = "hp",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["horsepower", "horsepower (us)", "hp(us)", "mechanical horsepower"],
                CommonTargets = ["kW", "W"],
                SuggestionPriority = 20,
                Tags = ["imperial", "automotive", "mechanical"],
                Description = "Mechanical horsepower (745.7 watts)"
            },
            new UnitDescriptor
            {
                Id = "ft·lb/min",
                DisplayName = "Foot-pound per minute",
                Symbol = "ft·lb/min",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["ft-lb/min", "foot-pounds per minute", "foot-pound per minute"],
                CommonTargets = ["W", "hp"],
                SuggestionPriority = 80,
                Tags = ["imperial", "mechanical", "engineering"],
                Description = "Imperial unit of power"
            },
            new UnitDescriptor
            {
                Id = "BTU/min",
                DisplayName = "British Thermal Unit per minute",
                Symbol = "BTU/min",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["btu/min", "british thermal units per minute", "british thermal unit per minute"],
                CommonTargets = ["W", "kW"],
                SuggestionPriority = 85,
                Tags = ["imperial", "thermal", "hvac"],
                Description = "Thermal power unit"
            }
        ];
    }

    // Conversion constants for units not directly in Units.NET Power
    private const double FtLbPerMinToWatt = 1.3558179483314004 / 60.0; // J / min -> W
    private const double BtuPerMinToWatt = 1055.05585262 / 60.0;        // J / min -> W (IT)

    private static Power CreatePower(double value, string unit) => unit switch
    {
        "W" => Power.FromWatts(value),
        "kW" => Power.FromKilowatts(value),
        "hp" => Power.FromMechanicalHorsepower(value),
        "ft·lb/min" => Power.FromWatts(value * FtLbPerMinToWatt),
        "BTU/min" => Power.FromWatts(value * BtuPerMinToWatt),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Power p, string unit) => unit switch
    {
        "W" => (double)p.Watts,
        "kW" => (double)p.Kilowatts,
        "hp" => (double)p.MechanicalHorsepower,
        "ft·lb/min" => (double)p.Watts / FtLbPerMinToWatt,
        "BTU/min" => (double)p.Watts / BtuPerMinToWatt,
        _ => null
    };
}
