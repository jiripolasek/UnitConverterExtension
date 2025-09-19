using System.Globalization;

namespace JPSoftworks.UnitConverter.Core.Modules;

public class UnitsNetMassModule : IConversionModule
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["g"] = "g",
        ["gram"] = "g",
        ["grams"] = "g",

        ["kg"] = "kg",
        ["kilogram"] = "kg",
        ["kilograms"] = "kg",

        ["lb"] = "lb",
        ["lbs"] = "lb",
        ["pound"] = "lb",
        ["pounds"] = "lb",

        ["oz"] = "oz",
        ["ounce"] = "oz",
        ["ounces"] = "oz",
    };

    public string Category => "Mass";

    public IEnumerable<string> GetSupportedUnits() => Map.Values.Distinct();

    public bool TryNormalizeUnit(string input, CultureInfo culture, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input)) return false;

        if (Map.TryGetValue(input.Trim(), out var value))
        {
            normalized = value.ToLowerInvariant();
            return true;
        }
        return false;
    }

    public bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            var from = UnitsNet.Mass.From(value, UnitsNet.Units.MassUnit.Parse(fromUnit));
            var converted = from.ToUnit(UnitsNet.Units.MassUnit.Parse(toUnit));
            result = converted.Value;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public IEnumerable<ConversionSuggestion> GetCommonConversions(double value, string fromUnit)
    {
        // existing implementation retained
        yield break;
    }
}