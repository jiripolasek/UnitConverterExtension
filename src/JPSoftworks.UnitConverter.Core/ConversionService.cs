using System.Globalization;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core;

public sealed class ConversionService
{
    private readonly IConversionModule[] _modules;

    public ConversionService()
    {
        _modules =
        [
            new UnitsNetLengthModule(),
            new UnitsNetTemperatureModule(),
            new UnitsNetTimeModule(),
            new UnitsNetVolumeModule(),
            new UnitsNetMassModule()
        ];
    }

    public ConversionResult ConvertQuery(string query, CultureInfo culture)
    {
        var parser = new ConversionQueryParser();
        var parsed = parser.TryParseQuery(query, culture);
        if (parsed is null)
            return ConversionResult.Failure("Could not parse the query.");

        if (parsed.IsImplicitConversion)
        {
            // Find module supporting fromUnit and return suggestions
            var module = FindModuleForUnit(parsed.FromUnit, culture, out var normalized);
            if (module is null)
                return ConversionResult.Failure($"Unknown unit: {parsed.FromUnit}");

            var suggestions = module.GetCommonConversions(parsed.Value, normalized!).ToArray();
            if (suggestions.Length == 0)
                return ConversionResult.Failure("No suggestions available.");

            return ConversionResult.SuccessSuggestions(parsed.Value, parsed.FromUnit, suggestions);
        }
        else
        {
            // Try direct same-module conversion
            var fromModule = FindModuleForUnit(parsed.FromUnit, culture, out var fromNormalized);
            var toModule = FindModuleForUnit(parsed.ToUnit, culture, out var toNormalized);

            if (fromModule is not null && ReferenceEquals(fromModule, toModule))
            {
                if (TryConvertWithDimension(fromModule, parsed.Dimension, parsed.Value, fromNormalized!, toNormalized!, out var v))
                {
                    return ConversionResult.Success(v, parsed.ToUnit);
                }
                return ConversionResult.Failure($"Cannot convert between {parsed.FromUnit} and {parsed.ToUnit}.");
            }

            // Special bridge: cubic length <-> volume (e.g., m³ <-> l)
            if (parsed.Dimension == UnitDimension.Cubic &&
                fromModule is UnitsNetLengthModule lengthModule &&
                toModule is UnitsNetVolumeModule volumeModule)
            {
                if (TryCubicLengthToVolumeBridge(lengthModule, volumeModule, parsed.Value, fromNormalized!, toNormalized!, out var v))
                    return ConversionResult.Success(v, parsed.ToUnit);

                return ConversionResult.Failure($"Cannot convert between {parsed.FromUnit} and {parsed.ToUnit}.");
            }

            if (parsed.Dimension == UnitDimension.Cubic &&
                fromModule is UnitsNetVolumeModule volumeModule2 &&
                toModule is UnitsNetLengthModule lengthModule2)
            {
                if (TryVolumeToCubicLengthBridge(lengthModule2, volumeModule2, parsed.Value, fromNormalized!, toNormalized!, out var v))
                    return ConversionResult.Success(v, parsed.ToUnit);

                return ConversionResult.Failure($"Cannot convert between {parsed.FromUnit} and {parsed.ToUnit}.");
            }

            return ConversionResult.Failure($"Cannot convert between {fromNormalized ?? parsed.FromUnit} and {toNormalized ?? parsed.ToUnit} - they are different unit types.");
        }
    }

    public Dictionary<string, string[]> GetAllSupportedUnits()
    {
        return _modules
            .GroupBy(m => m.Category)
            .ToDictionary(g => g.Key, g => g.SelectMany(m => m.GetSupportedUnits()).Distinct().ToArray());
    }

    private static bool TryConvertWithDimension(IConversionModule module, UnitDimension dim, double value, string fromUnit, string toUnit, out double result)
    {
        // For linear conversions, delegate directly
        if (dim == UnitDimension.Linear)
            return module.TryConvert(value, fromUnit, toUnit, out result);

        // For area/volume conversions, leverage linear conversion factor and apply exponent
        // We compute factor by converting 1 fromUnit to toUnit linearly and then apply exponent to value.
        // Note: This assumes module supports linear from/to units; flags SupportsSquaredUnits/SupportsCubicUnits are for capability, not API.
        if (!module.TryConvert(1.0, fromUnit, toUnit, out var linearFactor))
        {
            result = 0;
            return false;
        }

        var exponent = dim == UnitDimension.Squared ? 2 : 3;
        result = value * Math.Pow(linearFactor, exponent);
        return true;
    }

    private bool TryCubicLengthToVolumeBridge(UnitsNetLengthModule lengthModule, UnitsNetVolumeModule volumeModule, double value, string fromLengthUnit, string toVolumeUnit, out double result)
    {
        result = 0;

        // Convert 1 fromLengthUnit -> meters to get linear factor
        if (!lengthModule.TryConvert(1.0, fromLengthUnit, "m", out var toMeters))
            return false;

        // Value in cubic meters
        var cubicMeters = value * Math.Pow(toMeters, 3);

        // Convert cubic meters -> liters/ml
        if (IsLiter(toVolumeUnit))
        {
            result = cubicMeters * 1000.0; // 1 m³ = 1000 L
            return true;
        }
        if (IsMilliLiter(toVolumeUnit))
        {
            result = cubicMeters * 1_000_000.0; // 1 m³ = 1e6 mL
            return true;
        }

        // If volume module supports "m3" unit natively, try it
        if (volumeModule.TryConvert(cubicMeters, "m3", toVolumeUnit, out var v))
        {
            result = v;
            return true;
        }

        return false;
    }

    private bool TryVolumeToCubicLengthBridge(UnitsNetLengthModule lengthModule, UnitsNetVolumeModule volumeModule, double value, string fromVolumeUnit, string toLengthUnit, out double result)
    {
        result = 0;

        // Convert volume -> cubic meters
        double cubicMeters;
        if (IsLiter(fromVolumeUnit))
        {
            cubicMeters = value / 1000.0;
        }
        else if (IsMilliLiter(fromVolumeUnit))
        {
            cubicMeters = value / 1_000_000.0;
        }
        else if (volumeModule.TryConvert(value, fromVolumeUnit, "m3", out var m3))
        {
            cubicMeters = m3;
        }
        else
        {
            return false;
        }

        // Now go from cubic meters to target length unit (linear), then apply exponent inverse:
        // cubicMeters = v * (factor_to_m)^3 => v = cubicMeters / (factor_to_m)^3
        if (!lengthModule.TryConvert(1.0, toLengthUnit, "m", out var toMeters))
            return false;

        var denom = Math.Pow(toMeters, 3);
        if (denom == 0)
            return false;

        result = cubicMeters / denom;
        return true;
    }

    private static bool IsLiter(string unit) => unit.Equals("l", StringComparison.OrdinalIgnoreCase) || unit.Equals("liter", StringComparison.OrdinalIgnoreCase) || unit.Equals("litre", StringComparison.OrdinalIgnoreCase);
    private static bool IsMilliLiter(string unit) => unit.Equals("ml", StringComparison.OrdinalIgnoreCase) || unit.Equals("milliliter", StringComparison.OrdinalIgnoreCase) || unit.Equals("millilitre", StringComparison.OrdinalIgnoreCase);

    private IConversionModule? FindModuleForUnit(string unit, CultureInfo culture, out string? normalized)
    {
        normalized = null;
        foreach (var m in _modules)
        {
            if (m.TryNormalizeUnit(unit, culture, out var n))
            {
                normalized = n;
                return m;
            }
        }
        return null;
    }
}