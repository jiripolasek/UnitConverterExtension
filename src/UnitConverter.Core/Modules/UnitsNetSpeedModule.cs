// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Speed and pace conversion module powered by Units.NET using the UnitDescriptor system
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

    public override bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        // Keep the SI symbol kN (kilonewton) distinct from kn (knot).
        if (unit.Trim() == "kN")
        {
            normalizedUnit = string.Empty;
            return false;
        }

        return base.TryNormalizeUnit(unit, culture, out normalizedUnit);
    }

    internal static bool IsPaceUnit(string unit) => unit is "min/km" or "min/mi" or "s/km" or "s/mi";

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            if (!double.IsFinite(value) || !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
                !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            {
                return false;
            }

            var involvesPace = IsPaceUnit(from) || IsPaceUnit(to);
            if (involvesPace && value <= 0)
                return false;

            var speed = CreateSpeed(value, from);
            var converted = ConvertToUnit(speed, to);
            if (converted.HasValue && double.IsFinite(converted.Value) && (!involvesPace || converted.Value > 0))
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
                CommonTargets = ["km/h", "mph", "ft/s", "min/km", "min/mi"],
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
                CommonTargets = ["m/s", "mph", "min/km", "min/mi"],
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
                CommonTargets = ["km/h", "m/s", "min/mi", "min/km"],
                SuggestionPriority = 5,
                Tags = ["imperial", "common", "automotive"]
            },
            new UnitDescriptor
            {
                Id = "min/km", DisplayName = "Minute per kilometer", Symbol = "min/km", System = "Metric",
                Aliases = ["minute per kilometer", "minutes per kilometer", "minute per kilometre", "minutes per kilometre"],
                CommonTargets = ["min/mi", "km/h", "mph", "s/km"], SuggestionPriority = 20
            },
            new UnitDescriptor
            {
                Id = "min/mi", DisplayName = "Minute per mile", Symbol = "min/mi", System = "Imperial",
                Aliases = ["min/mile", "minute per mile", "minutes per mile"],
                CommonTargets = ["min/km", "mph", "km/h", "s/mi"], SuggestionPriority = 20
            },
            new UnitDescriptor
            {
                Id = "s/km", DisplayName = "Second per kilometer", Symbol = "s/km", System = "Metric",
                Aliases = ["sec/km", "second per kilometer", "seconds per kilometer", "second per kilometre", "seconds per kilometre"],
                CommonTargets = ["min/km", "min/mi", "km/h", "s/mi"], SuggestionPriority = 40
            },
            new UnitDescriptor
            {
                Id = "s/mi", DisplayName = "Second per mile", Symbol = "s/mi", System = "Imperial",
                Aliases = ["s/mile", "sec/mi", "second per mile", "seconds per mile"],
                CommonTargets = ["min/mi", "min/km", "mph", "s/km"], SuggestionPriority = 40
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
        "min/km" => Speed.FromKilometersPerHour(60 / value),
        "min/mi" => Speed.FromMilesPerHour(60 / value),
        "s/km" => Speed.FromKilometersPerHour(3600 / value),
        "s/mi" => Speed.FromMilesPerHour(3600 / value),
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
        "min/km" => 60 / s.KilometersPerHour,
        "min/mi" => 60 / s.MilesPerHour,
        "s/km" => 3600 / s.KilometersPerHour,
        "s/mi" => 3600 / s.MilesPerHour,
        "kn" => s.Knots,
        "Mach" => s.Mach,
        _ => null
    };
}