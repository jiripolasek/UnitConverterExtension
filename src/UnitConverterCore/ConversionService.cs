// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core;

public class ConversionService
{
    private readonly List<IConversionModule> _modules;
    private readonly ConversionQueryParser _parser;
    private readonly IAccuracyPreferenceProvider _accuracy;

    public IEnumerable<IConversionModule> Modules => this._modules;

    public ConversionService() : this(null, null)
    {
    }

    public ConversionService(IUnitPreferenceProvider? preferences) : this(preferences, null)
    {
    }

    public ConversionService(IUnitPreferenceProvider? unitPreferences, IAccuracyPreferenceProvider? accuracyPreferences)
    {
        this._parser = new ConversionQueryParser();
        var pref = unitPreferences ?? new DefaultUnitPreferenceProvider();
        _accuracy = accuracyPreferences ?? new DefaultAccuracyPreferenceProvider();
        this._modules =
        [
            new UnitsNetLengthModule(),
            new UnitsNetTemperatureModule(),
            new UnitsNetTimeModule(),
            new UnitsNetVolumeModule(pref),
            new UnitsNetMassModule(),
            new UnitsNetSpeedModule(),
            new UnitsNetPowerModule(),
            new UnitsNetEnergyModule(),
            new UnitsNetPressureModule(),
            new UnitsNetAngleModule(),
            new UnitsNetDataModule(),
            new UnitsNetAreaModule()
        ];
    }

    /// <summary>
    ///     Converts a unit conversion query and returns the result
    /// </summary>
    public ConversionResult ConvertQuery(string query, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;

        // Parse the query
        var parsedQuery = this._parser.TryParseQuery(query, culture);
        if (parsedQuery == null)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage
                    = "Could not parse the conversion query. Please use formats like '1 m to cm' or '60 mins'."
            };
        }

        // Handle implicit conversions (suggestions only)
        if (parsedQuery.IsImplicitConversion)
        {
            return this.HandleImplicitConversion(parsedQuery, culture);
        }

        // Handle explicit conversions
        return this.HandleExplicitConversion(parsedQuery, culture);
    }

    /// <summary>
    ///     Gets all supported unit categories and their units
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string[]>> GetAllSupportedUnits()
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, string[]>>();
        foreach (var module in this._modules)
        {
            result[module.Category] = module.GetSupportedUnits();
        }

        return result;
    }

    private ConversionResult HandleExplicitConversion(ParsedConversionQuery parsed, CultureInfo culture)
    {
        // Try to find a single module that can handle both units first
        var unifiedModule = this.FindUnifiedModuleForUnits(parsed.FromUnit, parsed.ToUnit, culture, 
            out var normalizedFromUnit, out var normalizedToUnit);
        
        if (unifiedModule != null)
        {
            if (TryConvertWithDimension(unifiedModule, parsed.Dimension, parsed.Value, normalizedFromUnit, normalizedToUnit, out var v))
            {
                var type = ResolveUnitType(unifiedModule, parsed.Dimension);
                var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, culture);
                var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = parsed.Dimension, Metadata = metadata };
                return new ConversionResult
                {
                    Success = true,
                    Output = new Quantity(v, token),
                    FormattedResult = this.FormatResult(v, normalizedToUnit, parsed.Dimension, culture)
                };
            }

            return new ConversionResult
            {
                Success = false, ErrorMessage = $"Failed to convert from {parsed.FromUnit} to {parsed.ToUnit}."
            };
        }

        // Fall back to separate module resolution
        var fromModule = this.FindModuleForUnit(parsed.FromUnit, culture, out normalizedFromUnit);
        var toModule = this.FindModuleForUnit(parsed.ToUnit, culture, out normalizedToUnit);

        // If ambiguity blocked module resolution, provide a friendly error
        if (fromModule == null && this._modules.OfType<DescriptorBasedConversionModule>()
            .Any(m => m.TryGetAmbiguity(parsed.FromUnit, out _, out _)))
        {
            var providerModule = _modules.OfType<DescriptorBasedConversionModule>()
                .FirstOrDefault(m => m.TryGetAmbiguity(parsed.FromUnit, out _, out _));
            if (providerModule != null && providerModule.TryGetAmbiguity(parsed.FromUnit, out var cands, out var reason))
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = $"{reason} Please specify '{string.Join("' or '", cands)}'."
                };
            }
        }

        if (toModule == null && this._modules.OfType<DescriptorBasedConversionModule>()
            .Any(m => m.TryGetAmbiguity(parsed.ToUnit, out _, out _)))
        {
            var providerModule = _modules.OfType<DescriptorBasedConversionModule>()
                .FirstOrDefault(m => m.TryGetAmbiguity(parsed.ToUnit, out _, out _));
            if (providerModule != null && providerModule.TryGetAmbiguity(parsed.ToUnit, out var cands, out var reason))
            {
                return new ConversionResult
                {
                    Success = false,
                    ErrorMessage = $"{reason} Please specify '{string.Join("' or '", cands)}'."
                };
            }
        }

        if (fromModule == null || toModule == null)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = fromModule == null
                    ? $"Unknown unit: {parsed.FromUnit}"
                    : $"Unknown unit: {parsed.ToUnit}"
            };
        }

        // Same module: try within-module conversion, with dimensional handling
        if (ReferenceEquals(fromModule, toModule))
        {
            if (TryConvertWithDimension(fromModule, parsed.Dimension, parsed.Value, normalizedFromUnit, normalizedToUnit,
                    out var v))
            {
                var type = ResolveUnitType(fromModule, parsed.Dimension);
                var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, culture);
                var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = parsed.Dimension, Metadata = metadata };
                return new ConversionResult
                {
                    Success = true,
                    Output = new Quantity(v, token),
                    FormattedResult = this.FormatResult(v, normalizedToUnit, parsed.Dimension, culture)
                };
            }

            return new ConversionResult
            {
                Success = false, ErrorMessage = $"Failed to convert from {parsed.FromUnit} to {parsed.ToUnit}."
            };
        }

        // Bridge conversions for cubic Length <-> Volume
        if (parsed.Dimension == UnitDimension.Cubic)
        {
            if (fromModule is UnitsNetLengthModule lengthModule && toModule is UnitsNetVolumeModule volumeModule)
            {
                if (TryCubicLengthToVolumeBridge(lengthModule, volumeModule, parsed.Value, normalizedFromUnit,
                        normalizedToUnit, out var v))
                {
                    var type = ResolveUnitType(volumeModule, parsed.Dimension);
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, culture);
                    var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = parsed.Dimension, Metadata = metadata };
                    return new ConversionResult
                    {
                        Success = true,
                        Output = new Quantity(v, token),
                        FormattedResult = this.FormatResult(v, normalizedToUnit, parsed.Dimension, culture)
                    };
                }
            }
            else if (fromModule is UnitsNetVolumeModule volumeModule2 && toModule is UnitsNetLengthModule lengthModule2)
            {
                if (TryVolumeToCubicLengthBridge(lengthModule2, volumeModule2, parsed.Value, normalizedFromUnit,
                        normalizedToUnit, out var v))
                {
                    var type = ResolveUnitType(lengthModule2, parsed.Dimension);
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, culture);
                    var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = parsed.Dimension, Metadata = metadata };
                    return new ConversionResult
                    {
                        Success = true,
                        Output = new Quantity(v, token),
                        FormattedResult = this.FormatResult(v, normalizedToUnit, parsed.Dimension, culture)
                    };
                }
            }
        }
        // Bridge conversions for squared Length <-> Area
        else if (parsed.Dimension == UnitDimension.Squared)
        {
            if (fromModule is UnitsNetLengthModule lengthModule3 && toModule is UnitsNetAreaModule areaModule)
            {
                if (TrySquaredLengthToAreaBridge(lengthModule3, areaModule, parsed.Value, normalizedFromUnit, normalizedToUnit, out var v))
                {
                    var type = ResolveUnitType(areaModule, parsed.Dimension);
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, culture);
                    // For dedicated area units (ha, acre, m2, ft2), the symbol already encodes the squared dimension.
                    // Set token dimension to Linear to avoid duplicating the ² suffix in formatting.
                    var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = UnitDimension.Linear, Metadata = metadata };
                    return new ConversionResult
                    {
                        Success = true,
                        Output = new Quantity(v, token),
                        FormattedResult = this.FormatResult(v, normalizedToUnit, UnitDimension.Linear, culture)
                    };
                }
            }
            else if (fromModule is UnitsNetAreaModule areaModule2 && toModule is UnitsNetLengthModule lengthModule4)
            {
                if (TryAreaToSquaredLengthBridge(lengthModule4, areaModule2, parsed.Value, normalizedFromUnit, normalizedToUnit, out var v))
                {
                    var type = ResolveUnitType(lengthModule4, parsed.Dimension);
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, culture);
                    var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = parsed.Dimension, Metadata = metadata };
                    return new ConversionResult
                    {
                        Success = true,
                        Output = new Quantity(v, token),
                        FormattedResult = this.FormatResult(v, normalizedToUnit, parsed.Dimension, culture)
                    };
                }
            }
        }

        return new ConversionResult
        {
            Success = false,
            ErrorMessage
                = $"Cannot convert between {parsed.FromUnit} and {parsed.ToUnit} - they are different unit types."
        };
    }

    private ConversionResult HandleImplicitConversion(ParsedConversionQuery query, CultureInfo culture)
    {
        // Find the module for the unit
        var module = this.FindModuleForUnit(query.FromUnit, culture, out var normalizedUnit);
        if (module == null)
        {
            return new ConversionResult { Success = false, ErrorMessage = $"Unknown unit: {query.FromUnit}" };
        }

        // Get suggestions
        var suggestions = module.GetCommonConversions(query.Value, normalizedUnit).ToArray();

        // If dimension is squared/cubic, recompute suggestions using the proper exponent
        if (query.Dimension != UnitDimension.Linear)
        {
            var adjusted = new List<ConversionSuggestion>();
            foreach (var s in suggestions)
            {
                var targetUnit = s.Output.Unit.Unit;
                if (TryConvertWithDimension(module, query.Dimension, query.Value, normalizedUnit, targetUnit, out var converted))
                {
                    var type = ResolveUnitType(module, query.Dimension);
                    var metadata = UnitMetadataProvider.GetMetadata(targetUnit, this._modules, culture);
                    var token = new UnitToken { Unit = targetUnit, Type = type, Dimension = query.Dimension, Metadata = metadata };
                    var formatted = this.FormatResult(converted, targetUnit, query.Dimension, culture);
                    adjusted.Add(new ConversionSuggestion(new Quantity(converted, token), formatted));
                }
            }
            suggestions = adjusted.ToArray();
        }

        var type2 = ResolveUnitType(module, query.Dimension);
        var metadata2 = UnitMetadataProvider.GetMetadata(normalizedUnit, this._modules, culture);
        var token2 = new UnitToken { Unit = normalizedUnit, Type = type2, Dimension = query.Dimension, Metadata = metadata2 };
        return new ConversionResult
        {
            Success = true,
            Output = new Quantity(query.Value, token2),
            FormattedResult = this.FormatResult(query.Value, normalizedUnit, query.Dimension, culture),
            Suggestions = suggestions
        };
    }

    private IConversionModule? FindModuleForUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        normalizedUnit = string.Empty;

        foreach (var module in this._modules)
        {
            if (module is DescriptorBasedConversionModule desc && desc.TryGetAmbiguity(unit, out var candidates, out var reason))
            {
                normalizedUnit = string.Empty;
                return null;
            }

            if (module.TryNormalizeUnit(unit, culture, out normalizedUnit))
            {
                return module;
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to find a single module that can handle both units, which helps resolve ambiguity
    /// when multiple modules contain units with the same name (e.g., "w" for week vs watt)
    /// </summary>
    private IConversionModule? FindUnifiedModuleForUnits(string fromUnit, string toUnit, CultureInfo culture, 
        out string normalizedFromUnit, out string normalizedToUnit)
    {
        normalizedFromUnit = string.Empty;
        normalizedToUnit = string.Empty;

        foreach (var module in this._modules)
        {
            if (module.TryNormalizeUnit(fromUnit, culture, out var normalizedFrom) &&
                module.TryNormalizeUnit(toUnit, culture, out var normalizedTo))
            {
                normalizedFromUnit = normalizedFrom;
                normalizedToUnit = normalizedTo;
                return module;
            }
        }

        return null;
    }

    private static bool TryConvertWithDimension(
        IConversionModule module,
        UnitDimension dim,
        double value,
        string fromUnit,
        string toUnit,
        out double result)
    {
        // Linear: direct
        if (dim == UnitDimension.Linear)
        {
            return module.TryConvert(value, fromUnit, toUnit, out result);
        }

        // For squared/cubic within a single module that operates on linear units,
        // compute the linear factor and apply exponent.
        if (!module.TryConvert(1.0, fromUnit, toUnit, out var linearFactor))
        {
            result = 0;
            return false;
        }

        var exponent = dim == UnitDimension.Squared ? 2 : 3;
        result = value * Math.Pow(linearFactor, exponent);
        return true;
    }

    private static bool TryCubicLengthToVolumeBridge(
        UnitsNetLengthModule lengthModule,
        UnitsNetVolumeModule volumeModule,
        double value,
        string fromLengthUnit,
        string toVolumeUnit,
        out double result)
    {
        result = 0;

        // Convert 1 fromLengthUnit -> meters to get linear factor
        if (!lengthModule.TryConvert(1.0, fromLengthUnit, "m", out var toMeters))
        {
            return false;
        }

        // Value in cubic meters
        var cubicMeters = value * Math.Pow(toMeters, 3);

        // Convert cubic meters -> liters first
        var liters = cubicMeters * 1000.0; // 1 m³ = 1000 L

        // If target is liters directly
        if (string.Equals(toVolumeUnit, "l", StringComparison.OrdinalIgnoreCase))
        {
            result = liters;
            return true;
        }

        // Otherwise, use volume module to convert liters -> target unit
        if (volumeModule.TryConvert(liters, "l", toVolumeUnit, out var v))
        {
            result = v;
            return true;
        }

        return false;
    }

    private static bool TryVolumeToCubicLengthBridge(
        UnitsNetLengthModule lengthModule,
        UnitsNetVolumeModule volumeModule,
        double value,
        string fromVolumeUnit,
        string toLengthUnit,
        out double result)
    {
        result = 0;

        // Convert fromVolumeUnit -> liters
        double liters;
        if (string.Equals(fromVolumeUnit, "l", StringComparison.OrdinalIgnoreCase))
        {
            liters = value;
        }
        else if (volumeModule.TryConvert(value, fromVolumeUnit, "l", out var l))
        {
            liters = l;
        }
        else
        {
            return false;
        }

        // liters -> cubic meters
        var cubicMeters = liters / 1000.0;

        // meters factor for target length unit
        if (!lengthModule.TryConvert(1.0, toLengthUnit, "m", out var toMeters))
        {
            return false;
        }

        var denom = Math.Pow(toMeters, 3);
        if (denom == 0)
        {
            return false;
        }

        result = cubicMeters / denom;
        return true;
    }

    private static bool TrySquaredLengthToAreaBridge(
        UnitsNetLengthModule lengthModule,
        UnitsNetAreaModule areaModule,
        double value,
        string fromLengthUnit,
        string toAreaUnit,
        out double result)
    {
        result = 0;

        // Convert 1 fromLengthUnit -> meters to get linear factor
        if (!lengthModule.TryConvert(1.0, fromLengthUnit, "m", out var toMeters))
        {
            return false;
        }

        // Value in square meters
        var squareMeters = value * Math.Pow(toMeters, 2);

        // If target is square meters directly
        if (string.Equals(toAreaUnit, "m2", StringComparison.OrdinalIgnoreCase))
        {
            result = squareMeters;
            return true;
        }

        // Otherwise, use area module to convert m2 -> target area unit
        if (areaModule.TryConvert(squareMeters, "m2", toAreaUnit, out var v))
        {
            result = v;
            return true;
        }

        return false;
    }

    private static bool TryAreaToSquaredLengthBridge(
        UnitsNetLengthModule lengthModule,
        UnitsNetAreaModule areaModule,
        double value,
        string fromAreaUnit,
        string toLengthUnit,
        out double result)
    {
        result = 0;

        // Convert fromAreaUnit -> square meters
        double squareMeters;
        if (string.Equals(fromAreaUnit, "m2", StringComparison.OrdinalIgnoreCase))
        {
            squareMeters = value;
        }
        else if (areaModule.TryConvert(value, fromAreaUnit, "m2", out var m2))
        {
            squareMeters = m2;
        }
        else
        {
            return false;
        }

        // meters factor for target length unit
        if (!lengthModule.TryConvert(1.0, toLengthUnit, "m", out var toMeters))
        {
            return false;
        }

        var denom = Math.Pow(toMeters, 2);
        if (denom == 0)
        {
            return false;
        }

        result = squareMeters / denom;
        return true;
    }

    private string FormatResult(double value, string unit, UnitDimension dimension, CultureInfo culture)
    {
        var abs = Math.Abs(value);
        var basePrecision = abs switch
        {
            < 0.001 => 6,
            < 0.01 => 4,
            < 0.1 => 3,
            < 1 => 2,
            < 10 => 2,
            < 1000 => 1,
            _ => 0
        };

        // Adjust with accuracy preference
        var precision = _accuracy.Accuracy switch
        {
            AccuracyLevel.CommonUse => basePrecision,
            AccuracyLevel.Enhanced => Math.Min(basePrecision + 2, 12),
            AccuracyLevel.ExtremelyPrecise => Math.Min(basePrecision + 4, 15),
            _ => basePrecision
        };

        var number = value.ToString($"F{precision}", culture);
        number = TrimZeros(number, culture);

        var dimensionSuffix = dimension switch
        {
            UnitDimension.Squared => "²",
            UnitDimension.Cubic => "³",
            _ => ""
        };

        var symbol = UnitMetadataProvider.GetSymbol(unit, this._modules, culture);
        return $"{number} {symbol}{dimensionSuffix}";
    }

    private static string TrimZeros(string s, CultureInfo culture)
    {
        var sep = culture.NumberFormat.NumberDecimalSeparator;
        if (s.Contains(sep, StringComparison.Ordinal))
        {
            s = s.TrimEnd('0');
            if (s.EndsWith(sep, StringComparison.Ordinal))
            {
                s = s[..^sep.Length];
            }
        }
        return s;
    }

    private static UnitType ResolveUnitType(IConversionModule module, UnitDimension dimension)
    {
        // Prefer specialized icons for derived dimensions of length
        if (module is UnitsNetLengthModule)
        {
            return dimension switch
            {
                UnitDimension.Squared => UnitType.Area,
                UnitDimension.Cubic => UnitType.Volume,
                _ => UnitType.Length
            };
        }

        return module.Category switch
        {
            "Temperature" => UnitType.Temperature,
            "Time" => UnitType.Time,
            "Volume" => UnitType.Volume,
            "Mass" => UnitType.Mass,
            "Length" => UnitType.Length,
            "Speed" => UnitType.Speed,
            "Power" => UnitType.Power,
            "Energy" => UnitType.Energy,
            "Pressure" => UnitType.Pressure,
            "Angle" => UnitType.Angle,
            "Data" => UnitType.Data,
            "Area" => UnitType.Area,
            _ => UnitType.Unknown
        };
    }
}

internal record ConversionInputModel
{
    internal string FromUnit { get; set; } = string.Empty;

    internal string ToUnit { get; set; } = string.Empty;

    internal double FromValue { get; set; }
}