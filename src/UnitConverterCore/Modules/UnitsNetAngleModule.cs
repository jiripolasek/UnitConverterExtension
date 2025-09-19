// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Angle conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetAngleModule : DescriptorBasedConversionModule
{
    public UnitsNetAngleModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Angle";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Angle;

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

            var angle = CreateAngle(value, from);
            var converted = ConvertToUnit(angle, to);
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
                Id = "deg",
                DisplayName = "Degree",
                Symbol = "°",
                System = "Universal",
                IsCommon = true,
                Aliases = ["degree", "degrees", "°"],
                CommonTargets = ["rad", "grad"],
                SuggestionPriority = 5,
                Tags = ["common", "navigation", "geometry"],
                Description = "1/360 of a full rotation"
            },
            new UnitDescriptor
            {
                Id = "rad",
                DisplayName = "Radian",
                Symbol = "rad",
                System = "Metric",
                IsCommon = true,
                Aliases = ["radian", "radians"],
                CommonTargets = ["deg", "grad"],
                SuggestionPriority = 10,
                Tags = ["SI", "mathematics", "physics"],
                Description = "SI unit of angular measure"
            },
            new UnitDescriptor
            {
                Id = "grad",
                DisplayName = "Gradian",
                Symbol = "grad",
                System = "Metric",
                IsCommon = false,
                Aliases = ["gradian", "gradians", "gon"],
                CommonTargets = ["deg", "rad"],
                SuggestionPriority = 80,
                Tags = ["surveying", "engineering"],
                Description = "1/400 of a full rotation"
            },
            new UnitDescriptor
            {
                Id = "turn",
                DisplayName = "Turn",
                Symbol = "turn",
                System = "Universal",
                IsCommon = false,
                Aliases = ["revolution", "revolutions", "rot"],
                CommonTargets = ["deg", "rad"],
                SuggestionPriority = 70,
                Tags = ["rotation", "mechanics"],
                Description = "One complete rotation (360°)"
            }
        ];
    }

    private static Angle CreateAngle(double value, string unit) => unit switch
    {
        "deg" => Angle.FromDegrees(value),
        "rad" => Angle.FromRadians(value),
        "grad" => Angle.FromGradians(value),
        "turn" => Angle.FromRevolutions(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Angle angle, string unit) => unit switch
    {
        "deg" => angle.Degrees,
        "rad" => angle.Radians,
        "grad" => angle.Gradians,
        "turn" => angle.Revolutions,
        _ => null
    };
}
