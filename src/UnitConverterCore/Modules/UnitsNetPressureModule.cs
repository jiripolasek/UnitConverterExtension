// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Pressure conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetPressureModule : DescriptorBasedConversionModule
{
    public UnitsNetPressureModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Pressure";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Pressure;

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

            var pressure = CreatePressure(value, from);
            var converted = ConvertToUnit(pressure, to);
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
                Id = "Pa",
                DisplayName = "Pascal",
                Symbol = "Pa",
                System = "Metric",
                IsCommon = true,
                Aliases = ["pascal", "pascals"],
                CommonTargets = ["kPa", "bar", "psi"],
                SuggestionPriority = 15,
                Tags = ["SI", "metric"],
                Description = "SI unit of pressure"
            },
            new UnitDescriptor
            {
                Id = "kPa",
                DisplayName = "Kilopascal",
                Symbol = "kPa",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilopascal", "kilopascals"],
                CommonTargets = ["Pa", "bar", "psi"],
                SuggestionPriority = 10,
                Tags = ["SI", "metric", "common"],
                Description = "1000 pascals"
            },
            new UnitDescriptor
            {
                Id = "bar",
                DisplayName = "Bar",
                Symbol = "bar",
                System = "Metric",
                IsCommon = true,
                Aliases = ["bars"],
                CommonTargets = ["kPa", "psi", "atm"],
                SuggestionPriority = 5,
                Tags = ["metric", "common", "atmospheric"],
                Description = "100,000 pascals"
            },
            new UnitDescriptor
            {
                Id = "atm",
                DisplayName = "Atmosphere",
                Symbol = "atm",
                System = "Physics",
                IsCommon = true,
                Aliases = ["atmosphere", "atmospheres"],
                CommonTargets = ["bar", "kPa", "psi"],
                SuggestionPriority = 20,
                Tags = ["atmospheric", "standard"],
                Description = "Standard atmospheric pressure"
            },
            new UnitDescriptor
            {
                Id = "psi",
                DisplayName = "Pound per square inch",
                Symbol = "psi",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["pounds per square inch", "pound per square inch"],
                CommonTargets = ["bar", "kPa", "atm"],
                SuggestionPriority = 8,
                Tags = ["imperial", "common", "US", "automotive"],
                Description = "Pressure per square inch"
            },
            new UnitDescriptor
            {
                Id = "mmHg",
                DisplayName = "Millimeter of mercury",
                Symbol = "mmHg",
                System = "Medical",
                IsCommon = true,
                Aliases = ["millimeters of mercury", "mm Hg", "torr"],
                CommonTargets = ["kPa", "psi"],
                SuggestionPriority = 25,
                Tags = ["medical", "blood-pressure", "vacuum"],
                Description = "Pressure equivalent to 1mm of mercury"
            },
            new UnitDescriptor
            {
                Id = "inHg",
                DisplayName = "Inch of mercury",
                Symbol = "inHg",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["inches of mercury", "in Hg"],
                CommonTargets = ["mmHg", "kPa"],
                SuggestionPriority = 60,
                Tags = ["imperial", "weather", "vacuum"],
                Description = "Pressure equivalent to 1 inch of mercury"
            }
        ];
    }

    private static Pressure CreatePressure(double value, string unit) => unit switch
    {
        "Pa" => Pressure.FromPascals(value),
        "kPa" => Pressure.FromKilopascals(value),
        "bar" => Pressure.FromBars(value),
        "atm" => Pressure.FromAtmospheres(value),
        "psi" => Pressure.FromPoundsForcePerSquareInch(value),
        "mmHg" => Pressure.FromMillimetersOfMercury(value),
        "inHg" => Pressure.FromInchesOfMercury(value),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Pressure pressure, string unit) => unit switch
    {
        "Pa" => pressure.Pascals,
        "kPa" => pressure.Kilopascals,
        "bar" => pressure.Bars,
        "atm" => pressure.Atmospheres,
        "psi" => pressure.PoundsForcePerSquareInch,
        "mmHg" => pressure.MillimetersOfMercury,
        "inHg" => pressure.InchesOfMercury,
        _ => null
    };
}
