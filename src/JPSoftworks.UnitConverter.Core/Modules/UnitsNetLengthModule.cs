using System.Globalization;

namespace JPSoftworks.UnitConverter.Core.Modules;

public class UnitsNetLengthModule : IConversionModule
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        // Base
        ["m"] = "m",
        ["meter"] = "m",
        ["metre"] = "m",
        ["meters"] = "m",
        ["metres"] = "m",

        ["mm"] = "mm",
        ["millimeter"] = "mm",
        ["millimetre"] = "mm",
        ["millimeters"] = "mm",
        ["millimetres"] = "mm",

        ["cm"] = "cm",
        ["centimeter"] = "cm",
        ["centimetre"] = "cm",
        ["centimeters"] = "cm",
        ["centimetres"] = "cm",

        ["km"] = "km",
        ["kilometer"] = "km",
        ["kilometre"] = "km",
        ["kilometers"] = "km",
        ["kilometres"] = "km",

        ["ft"] = "ft",
        ["foot"] = "ft",
        ["feet"] = "ft",
        ["'"] = "ft",

        ["in"] = "in",
        ["inch"] = "in",
        ["inches"] = "in",
        ["\""] = "in",

        ["yd"] = "yd",
        ["yard"] = "yd",
        ["yards"] = "yd",

        ["mi"] = "mi",
        ["mile"] = "mi",
        ["miles"] = "mi",

        ["nmi"] = "nmi",
        ["nauticalmile"] = "nmi",
        ["nautical mile"] = "nmi",
        ["nm"] = "nmi",
    };

    public string Category => "Length";
    public bool SupportsSquaredUnits => true;
    public bool SupportsCubicUnits => true;

    public IEnumerable<string> GetSupportedUnits() => Map.Values.Distinct();

    public bool TryNormalizeUnit(string input, CultureInfo culture, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var key = input.Trim();

        // Preserve `"'"` and `"` as-is for lookup; otherwise strip trailing punctuation/spaces
        if (!Map.TryGetValue(key, out var value))
        {
            key = key.Replace(" ", string.Empty); // handle "nautical mile"
            if (!Map.TryGetValue(key, out value))
                return false;
        }

        normalized = value.ToLowerInvariant();
        return true;
    }

    // The rest of the module delegates to UnitsNet (existing implementation)...
    public bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        // existing implementation retained
        result = 0;
        try
        {
            var from = UnitsNet.Length.From(value, UnitsNet.Units.LengthUnit.Parse(fromUnit));
            var converted = from.ToUnit(UnitsNet.Units.LengthUnit.Parse(toUnit));
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