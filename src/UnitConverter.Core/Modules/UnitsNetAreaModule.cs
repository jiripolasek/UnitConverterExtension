// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Area conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetAreaModule : DescriptorBasedConversionModule
{
    public UnitsNetAreaModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Area";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Area;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            if (!this.TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
                !this.TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            {
                return false;
            }

            var area = CreateArea(value, from);
            var converted = ConvertToUnit(area, to);
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
            // Metric base
            new UnitDescriptor
            {
                Id = "mm2",
                DisplayName = "Square Millimeter",
                Symbol = "mm²",
                System = "Metric",
                IsCommon = false,
                Aliases = ["mm^2", "mm²", "square millimeter", "square millimetre", "square millimeters", "square millimetres", "sq mm", "sqmm"],
                CommonTargets = ["cm2", "m2", "in2"],
                SuggestionPriority = 40,
                Tags = ["SI", "metric", "small"],
                Description = "1e-6 square meters"
            },
            new UnitDescriptor
            {
                Id = "cm2",
                DisplayName = "Square Centimeter",
                Symbol = "cm²",
                System = "Metric",
                IsCommon = true,
                Aliases = ["cm^2", "cm²", "square centimeter", "square centimetre", "square centimeters", "square centimetres", "sq cm", "sqcm"],
                CommonTargets = ["mm2", "m2", "in2"],
                SuggestionPriority = 20,
                Tags = ["SI", "metric", "common"],
                Description = "1e-4 square meters"
            },
            new UnitDescriptor
            {
                Id = "m2",
                DisplayName = "Square Meter",
                Symbol = "m²",
                System = "Metric",
                IsCommon = true,
                Aliases = ["m^2", "m²", "square meter", "square metre", "square meters", "square metres", "sq m", "sqm"],
                CommonTargets = ["cm2", "ft2", "yd2", "ha"],
                SuggestionPriority = 5,
                Tags = ["SI", "base", "metric", "common"],
                Description = "SI base unit of area"
            },
            new UnitDescriptor
            {
                Id = "km2",
                DisplayName = "Square Kilometer",
                Symbol = "km²",
                System = "Metric",
                IsCommon = true,
                Aliases = ["km^2", "km²", "square kilometer", "square kilometre", "square kilometers", "square kilometres", "sq km", "sqkm"],
                CommonTargets = ["m2", "mi2", "ha"],
                SuggestionPriority = 30,
                Tags = ["SI", "metric", "distance"],
                Description = "1,000,000 square meters"
            },

            // Imperial/US derived
            new UnitDescriptor
            {
                Id = "in2",
                DisplayName = "Square Inch",
                Symbol = "in²",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["in^2", "in²", "square inch", "square inches", "sq in", "sqin"],
                CommonTargets = ["cm2", "ft2"],
                SuggestionPriority = 15,
                Tags = ["imperial", "US", "common"],
                Description = "Area of a 1 in by 1 in square"
            },
            new UnitDescriptor
            {
                Id = "ft2",
                DisplayName = "Square Foot",
                Symbol = "ft²",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["ft^2", "ft²", "square foot", "square feet", "sq ft", "sqft"],
                CommonTargets = ["m2", "yd2", "in2"],
                SuggestionPriority = 10,
                Tags = ["imperial", "US", "common"],
                Description = "Area of a 1 ft by 1 ft square"
            },
            new UnitDescriptor
            {
                Id = "yd2",
                DisplayName = "Square Yard",
                Symbol = "yd²",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["yd^2", "yd²", "square yard", "square yards", "sq yd", "sqyd"],
                CommonTargets = ["m2", "ft2"],
                SuggestionPriority = 35,
                Tags = ["imperial", "US"],
                Description = "Area of a 1 yd by 1 yd square"
            },
            new UnitDescriptor
            {
                Id = "mi2",
                DisplayName = "Square Mile",
                Symbol = "mi²",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["mi^2", "mi²", "square mile", "square miles", "sq mi", "sqmi"],
                CommonTargets = ["km2", "acre"],
                SuggestionPriority = 45,
                Tags = ["imperial", "US", "distance"],
                Description = "Area of a 1 mi by 1 mi square"
            },

            // Dedicated area units
            new UnitDescriptor
            {
                Id = "ha",
                DisplayName = "Hectare",
                Symbol = "ha",
                System = "Metric",
                IsCommon = true,
                Aliases = ["hectare", "hectares"],
                CommonTargets = ["m2", "km2", "acre"],
                SuggestionPriority = 8,
                Tags = ["metric", "land"],
                Description = "10,000 square meters"
            },
            new UnitDescriptor
            {
                Id = "acre",
                DisplayName = "Acre",
                Symbol = "ac",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["acre", "acres", "ac"],
                CommonTargets = ["m2", "ha", "mi2"],
                SuggestionPriority = 12,
                Tags = ["imperial", "US", "land"],
                Description = "43,560 square feet (approx. 4046.8564224 m²)"
            },

            // Contextual units
            new UnitDescriptor
            {
                Id = "soccer field",
                DisplayName = "Soccer Field",
                Symbol = "soccer fld",
                System = "Contextual",
                IsCommon = false,
                Aliases = ["soccer field", "soccer pitch", "football pitch"],
                CommonTargets = ["m2", "ha", "acre"],
                SuggestionPriority = 90,
                Tags = ["context", "sports", "approx"],
                Description = "Approx. FIFA-recommended pitch 105 m × 68 m = 7,140 m²"
            },
            new UnitDescriptor
            {
                Id = "manhattan block",
                DisplayName = "Manhattan City Block",
                Symbol = "Manhattan blk",
                System = "Contextual",
                IsCommon = false,
                Aliases = ["manhattan block", "manhattan city block", "nyc block", "city block (manhattan)"],
                CommonTargets = ["m2", "ha", "acre", "ft2"],
                SuggestionPriority = 95,
                Tags = ["context", "urban", "approx"],
                Description = "Approx. 264 ft × 900 ft = 237,600 ft² ≈ 22,076 m²"
            }
        ];
    }

    private static Area CreateArea(double value, string unit) => unit switch
    {
        // Metric
        "mm2" => Area.FromSquareMillimeters(value),
        "cm2" => Area.FromSquareCentimeters(value),
        "m2" => Area.FromSquareMeters(value),
        "km2" => Area.FromSquareKilometers(value),
        // Imperial/US derived
        "in2" => Area.FromSquareInches(value),
        "ft2" => Area.FromSquareFeet(value),
        "yd2" => Area.FromSquareYards(value),
        "mi2" => Area.FromSquareMiles(value),
        // Dedicated
        "ha" => Area.FromHectares(value),
        "acre" => Area.FromAcres(value),
        // Contextual
        "soccer field" => Area.FromSquareMeters(value * 7140.0),
        "manhattan block" => Area.FromSquareFeet(value * 237_600.0),
        _ => throw new ArgumentOutOfRangeException(nameof(unit))
    };

    private static double? ConvertToUnit(Area area, string unit) => unit switch
    {
        // Metric
        "mm2" => area.SquareMillimeters,
        "cm2" => area.SquareCentimeters,
        "m2" => area.SquareMeters,
        "km2" => area.SquareKilometers,
        // Imperial/US derived
        "in2" => area.SquareInches,
        "ft2" => area.SquareFeet,
        "yd2" => area.SquareYards,
        "mi2" => area.SquareMiles,
        // Dedicated
        "ha" => area.Hectares,
        "acre" => area.Acres,
        // Contextual
        "soccer field" => area.SquareMeters / 7140.0,
        "manhattan block" => area.SquareFeet / 237_600.0,
        _ => null
    };
}
