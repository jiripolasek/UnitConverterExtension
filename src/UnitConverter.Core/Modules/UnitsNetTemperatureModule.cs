// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Temperature conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetTemperatureModule : DescriptorBasedConversionModule
{
    public UnitsNetTemperatureModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Temperature";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Temperature;

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

            var temperature = ParseTemperature(value, from);
            if (temperature.HasValue)
            {
                var converted = ConvertToUnit(temperature.Value, to);
                if (converted.HasValue)
                {
                    result = converted.Value;
                    return true;
                }
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
                Id = "°C",
                DisplayName = "Celsius",
                Symbol = "°C",
                System = "Metric",
                IsCommon = true,
                Aliases = ["celsius", "c", "centigrade"],
                CommonTargets = ["°F", "K"],
                SuggestionPriority = 5,
                Tags = ["metric", "common", "water-based"],
                Description = "Temperature scale based on water's freezing and boiling points"
            },
            new UnitDescriptor
            {
                Id = "°F",
                DisplayName = "Fahrenheit",
                Symbol = "°F",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["fahrenheit", "f"],
                CommonTargets = ["°C", "K"],
                SuggestionPriority = 10,
                Tags = ["imperial", "common", "US"],
                Description = "Temperature scale commonly used in the United States"
            },
            new UnitDescriptor
            {
                Id = "K",
                DisplayName = "Kelvin",
                Symbol = "K",
                System = "Metric",
                IsCommon = false,
                Aliases = ["kelvin", "k"],
                CommonTargets = ["°C", "°F"],
                SuggestionPriority = 30,
                Tags = ["SI", "absolute", "scientific"],
                Description = "Absolute temperature scale (SI base unit)"
            },
            new UnitDescriptor
            {
                Id = "°R",
                DisplayName = "Rankine",
                Symbol = "°R",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["rankine", "r"],
                CommonTargets = ["°F", "K"],
                SuggestionPriority = 80,
                Tags = ["imperial", "absolute", "engineering"],
                Description = "Absolute temperature scale based on Fahrenheit degrees"
            }
        ];
    }

    private static Temperature? ParseTemperature(double value, string unit)
    {
        try
        {
            return unit switch
            {
                "°C" => Temperature.FromDegreesCelsius(value),
                "°F" => Temperature.FromDegreesFahrenheit(value),
                "K" => Temperature.FromKelvins(value),
                "°R" => Temperature.FromDegreesRankine(value),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static double? ConvertToUnit(Temperature temperature, string unit)
    {
        return unit switch
        {
            "°C" => temperature.DegreesCelsius,
            "°F" => temperature.DegreesFahrenheit,
            "K" => temperature.Kelvins,
            "°R" => temperature.DegreesRankine,
            _ => null
        };
    }
}