// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Speed conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetSpeedModule : DescriptorBasedConversionModule
{
    public UnitsNetSpeedModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Speed";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Speed;

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

            var speed = CreateSpeed(value, from);
            var converted = ConvertToUnit(speed, to);
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
                Id = "cm/s",
                DisplayName = "Centimeter per second",
                Symbol = "cm/s",
                System = "Metric",
                IsCommon = false,
                Aliases = ["centimeters per second", "centimetres per second", "cmps"],
                CommonTargets = ["m/s", "km/h"],
                SuggestionPriority = 80,
                Tags = ["metric", "small"]
            },
            new UnitDescriptor
            {
                Id = "m/s",
                DisplayName = "Meter per second",
                Symbol = "m/s",
                System = "Metric",
                IsCommon = true,
                Aliases = ["meter per second", "meters per second", "metre per second", "metres per second", "mps"],
                CommonTargets = ["km/h", "mph", "ft/s"],
                SuggestionPriority = 10,
                Tags = ["metric", "SI", "base"]
            },
            new UnitDescriptor
            {
                Id = "km/h",
                DisplayName = "Kilometer per hour",
                Symbol = "km/h",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilometers per hour", "kilometres per hour", "kph", "kmh"],
                CommonTargets = ["m/s", "mph"],
                SuggestionPriority = 5,
                Tags = ["metric", "common", "automotive"]
            },
            new UnitDescriptor
            {
                Id = "ft/s",
                DisplayName = "Foot per second",
                Symbol = "ft/s",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["feet per second", "foot per second", "fps"],
                CommonTargets = ["m/s", "mph"],
                SuggestionPriority = 30,
                Tags = ["imperial", "engineering"]
            },
            new UnitDescriptor
            {
                Id = "mph",
                DisplayName = "Mile per hour",
                Symbol = "mph",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["miles per hour"],
                CommonTargets = ["km/h", "m/s"],
                SuggestionPriority = 5,
                Tags = ["imperial", "common", "automotive"]
            },
            new UnitDescriptor
            {
                Id = "kn",
                DisplayName = "Knot",
                Symbol = "kn",
                System = "Nautical",
                IsCommon = true,
                Aliases = ["knot", "knots", "kt", "kts"],
                CommonTargets = ["km/h", "mph"],
                SuggestionPriority = 40,
                Tags = ["nautical", "maritime", "aviation"]
            },
            new UnitDescriptor
            {
                Id = "Mach",
                DisplayName = "Mach number",
                Symbol = "Mach",
                System = "Physics",
                IsCommon = false,
                Aliases = ["mach"],
                CommonTargets = ["km/h", "mph", "m/s"],
                SuggestionPriority = 90,
                Tags = ["physics", "supersonic", "aviation"]
            }
        ];
    }

    private static Speed CreateSpeed(double value, string unit) => unit switch
    {
        "cm/s" => Speed.FromCentimetersPerSecond(value),
        "m/s" => Speed.FromMetersPerSecond(value),
        "km/h" => Speed.FromKilometersPerHour(value),
        "ft/s" => Speed.FromFeetPerSecond(value),
        "mph" => Speed.FromMilesPerHour(value),
        "kn" => Speed.FromKnots(value),
        "Mach" => Speed.FromMach(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Speed s, string unit) => unit switch
    {
        "cm/s" => s.CentimetersPerSecond,
        "m/s" => s.MetersPerSecond,
        "km/h" => s.KilometersPerHour,
        "ft/s" => s.FeetPerSecond,
        "mph" => s.MilesPerHour,
        "kn" => s.Knots,
        "Mach" => s.Mach,
        _ => null
    };
}
