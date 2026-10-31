// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core;

public partial class ConversionService
{
    private readonly List<IConversionModule> _modules;
    private readonly IAccuracyPreferenceProvider _accuracy;
    private readonly CultureInfo? _uiCulture;

    public IEnumerable<IConversionModule> Modules => this._modules;

    public ConversionService() : this(null, null, null)
    {
    }

    public ConversionService(IUnitPreferenceProvider? preferences) : this(preferences, null, null)
    {
    }

    public ConversionService(IUnitPreferenceProvider? unitPreferences, IAccuracyPreferenceProvider? accuracyPreferences)
        : this(unitPreferences, accuracyPreferences, null)
    {
    }

    public ConversionService(IUnitPreferenceProvider? unitPreferences, IAccuracyPreferenceProvider? accuracyPreferences, CultureInfo? uiCulture)
    {
        var pref = unitPreferences ?? new DefaultUnitPreferenceProvider();
        _accuracy = accuracyPreferences ?? new DefaultAccuracyPreferenceProvider();
        _uiCulture = uiCulture;
        this._modules =
        [
            new UnitsNetLengthModule(),
            new UnitsNetTemperatureModule(),
            new UnitsNetTemperatureDeltaModule(),
            new UnitsNetFrequencyModule(),
            new UnitsNetForceModule(),
            new UnitsNetTorqueModule(),
            new UnitsNetAccelerationModule(),
            new UnitsNetDensityModule(),
            new UnitsNetVoltageModule(),
            new UnitsNetCurrentModule(),
            new UnitsNetResistanceModule(),
            new UnitsNetTimeModule(),
            new UnitsNetVolumeModule(pref),
            new UnitsNetMassModule(),
            new UnitsNetSpeedModule(),
            new UnitsNetPowerModule(),
            new UnitsNetEnergyModule(),
            new UnitsNetPressureModule(),
            new UnitsNetAngleModule(),
            new UnitsNetDataModule(),
            new UnitsNetDataRateModule(),
            new UnitsNetFuelEconomyModule(),
            new UnitsNetAreaModule()
        ];
    }

    /// <summary>
    ///     Converts a unit conversion query and returns the result
    /// </summary>
    public ConversionResult ConvertQuery(string query, CultureInfo? culture = null) => ConvertQuery(query, culture, ConversionWordLanguage.Default);

    public ConversionResult ConvertQuery(string query, CultureInfo? culture, ConversionWordLanguage wordLanguage)
    {
        culture ??= CultureInfo.CurrentCulture;
        culture = ResolveQueryCulture(culture, wordLanguage);
        var result = EvaluateQuery(query, culture, wordLanguage);
        return result.Success || result.OtherInterpretations.Length > 0 ? result : AddGuidance(result, query, culture, wordLanguage);
    }

    private ConversionResult EvaluateExactQuery(string query, CultureInfo culture, ConversionWordLanguage wordLanguage)
    {
        var uiCulture = this._uiCulture ?? culture;
        // Parse the query
        var parsedQuery = ConversionQueryParser.TryParseQuery(query, culture, this._modules, wordLanguage);
        if (parsedQuery == null)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage
                    = ConverterText.Format("Could not parse the conversion query. Please use formats like '{0}' or '{1}'.", uiCulture,
                        FormatConversionQuery("1 m", "cm", culture, wordLanguage, uiCulture), "60 min")
            };
        }

        if (parsedQuery.Components.Length > 1)
        {
            parsedQuery = CombineComponents(parsedQuery, culture);
            if (parsedQuery == null)
                return new ConversionResult { ErrorMessage = ConverterText.Get("Mixed quantities must use compatible units and dimensions. Absolute temperatures and fuel economy cannot be added.", uiCulture) };
        }

        var result = parsedQuery.IsImplicitConversion
            ? this.HandleImplicitConversion(parsedQuery, culture)
            : this.HandleExplicitConversion(parsedQuery, culture);
        return result.Success && !double.IsFinite(result.Output.Value)
            ? new ConversionResult { ErrorMessage = ConverterText.Get("The result is outside the supported numeric range.", uiCulture) }
            : result;
    }

    private ParsedConversionQuery? CombineComponents(ParsedConversionQuery query, CultureInfo culture)
    {
        var module = FindModuleForUnit(query.FromUnit, culture, out var sourceUnit);
        if (module == null || module is UnitsNetTemperatureModule or UnitsNetFuelEconomyModule ||
            (query.ToDimension != UnitDimension.Linear && query.ToDimension != query.FromDimension))
            return null;

        var total = 0.0;
        foreach (var component in query.Components)
        {
            if (component.Unit.Dimension != query.FromDimension ||
                !module.TryNormalizeUnit(component.Unit.Unit, culture, out var unit) ||
                (module is UnitsNetSpeedModule && UnitsNetSpeedModule.IsPaceUnit(sourceUnit) != UnitsNetSpeedModule.IsPaceUnit(unit)) ||
                !TryConvertWithDimension(module, query.FromDimension, component.Value, unit, sourceUnit, out var converted))
                return null;

            total += converted;
        }

        return double.IsFinite(total)
            ? query with { Value = total, FromUnit = sourceUnit, Dimension = query.FromDimension, Components = [] }
            : null;
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

    public IEnumerable<ConversionSuggestion> GetAdditionalConversions(ConversionResult result, CultureInfo culture)
    {
        var uiCulture = this._uiCulture ?? culture;
        if (!result.Success || !double.IsFinite(result.Output.Value))
        {
            yield break;
        }

        var output = result.Output;
        var module = this._modules.FirstOrDefault(candidate => SupportsDimension(candidate, output.Unit.Dimension) &&
            ResolveUnitType(candidate, output.Unit.Dimension) == output.Unit.Type &&
            candidate.GetSupportedUnits().ContainsKey(output.Unit.Unit));
        if (module == null)
        {
            yield break;
        }

        foreach (var unit in module.GetAllUnitMetadata(uiCulture)
            .OrderBy(entry => !entry.Value.IsCommon)
            .ThenBy(entry => entry.Key.Length)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (unit.Key == output.Unit.Unit || result.Suggestions.Any(suggestion => suggestion.Output.Unit.Unit == unit.Key) ||
                !TryConvertWithDimension(module, output.Unit.Dimension, output.Value, output.Unit.Unit, unit.Key, out var converted) ||
                !double.IsFinite(converted))
            {
                continue;
            }

            var token = output.Unit with { Unit = unit.Key, Metadata = unit.Value };
            yield return new ConversionSuggestion(new Quantity(converted, token), this.FormatResult(converted, unit.Key, token.Dimension, culture));
        }
    }

    private ConversionResult HandleExplicitConversion(ParsedConversionQuery parsed, CultureInfo culture)
    {
        var uiCulture = this._uiCulture ?? culture;
        // Try to find a single module that can handle both units first
        var unifiedModule = this.FindUnifiedModuleForUnits(parsed.FromUnit, parsed.ToUnit, culture, 
            out var normalizedFromUnit, out var normalizedToUnit);
        
        if (unifiedModule != null)
        {
            if (TryConvertWithDimension(unifiedModule, parsed.Dimension, parsed.Value, normalizedFromUnit, normalizedToUnit, out var v))
            {
                var type = ResolveUnitType(unifiedModule, parsed.Dimension);
                var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, uiCulture);
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
                Success = false, ErrorMessage = ConverterText.Format("Failed to convert from {0} to {1}.", uiCulture, parsed.FromUnit, parsed.ToUnit)
            };
        }

        // Fall back to separate module resolution
        var fromModule = this.FindModuleForUnit(parsed.FromUnit, culture, out normalizedFromUnit);
        var toModule = this.FindModuleForUnit(parsed.ToUnit, culture, out normalizedToUnit);

        if (fromModule != null && toModule == null && parsed.Dimension == UnitDimension.Linear)
        {
            var compound = TryConvertCompound(parsed, fromModule, normalizedFromUnit, culture);
            if (compound != null)
                return compound;
        }

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
                    ErrorMessage = ConverterText.Format("{0} Please specify '{1}'.", uiCulture, ConverterText.Get(reason, uiCulture), string.Join($"' {ConverterText.Get("or", uiCulture)} '", cands))
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
                    ErrorMessage = ConverterText.Format("{0} Please specify '{1}'.", uiCulture, ConverterText.Get(reason, uiCulture), string.Join($"' {ConverterText.Get("or", uiCulture)} '", cands))
                };
            }
        }

        if (fromModule == null || toModule == null)
        {
            return new ConversionResult
            {
                Success = false,
                ErrorMessage = fromModule == null
                    ? ConverterText.Format("Unknown unit: {0}", uiCulture, parsed.FromUnit)
                    : ConverterText.Format("Unknown unit: {0}", uiCulture, parsed.ToUnit)
            };
        }

        // Same module: try within-module conversion, with dimensional handling
        if (ReferenceEquals(fromModule, toModule))
        {
            if (TryConvertWithDimension(fromModule, parsed.Dimension, parsed.Value, normalizedFromUnit, normalizedToUnit,
                    out var v))
            {
                var type = ResolveUnitType(fromModule, parsed.Dimension);
                var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, uiCulture);
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
                Success = false, ErrorMessage = ConverterText.Format("Failed to convert from {0} to {1}.", uiCulture, parsed.FromUnit, parsed.ToUnit)
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
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, uiCulture);
                    var token = new UnitToken { Unit = normalizedToUnit, Type = type, Dimension = UnitDimension.Linear, Metadata = metadata };
                    return new ConversionResult
                    {
                        Success = true,
                        Output = new Quantity(v, token),
                        FormattedResult = this.FormatResult(v, normalizedToUnit, UnitDimension.Linear, culture)
                    };
                }
            }
            else if (fromModule is UnitsNetVolumeModule volumeModule2 && toModule is UnitsNetLengthModule lengthModule2)
            {
                if (TryVolumeToCubicLengthBridge(lengthModule2, volumeModule2, parsed.Value, normalizedFromUnit,
                        normalizedToUnit, out var v))
                {
                    var type = ResolveUnitType(lengthModule2, parsed.Dimension);
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, uiCulture);
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
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, uiCulture);
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
                    var metadata = UnitMetadataProvider.GetMetadata(normalizedToUnit, this._modules, uiCulture);
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
                = ConverterText.Format("Cannot convert between {0} and {1} - they are different unit types.", uiCulture, parsed.FromUnit, parsed.ToUnit)
        };
    }

    private ConversionResult HandleImplicitConversion(ParsedConversionQuery query, CultureInfo culture)
    {
        var uiCulture = this._uiCulture ?? culture;
        // Find the module for the unit
        var module = this.FindModuleForUnit(query.FromUnit, culture, out var normalizedUnit);
        if (module == null)
        {
            var ambiguous = this._modules.OfType<DescriptorBasedConversionModule>()
                .FirstOrDefault(candidate => candidate.TryGetAmbiguity(query.FromUnit, out _, out _));
            if (ambiguous != null && ambiguous.TryGetAmbiguity(query.FromUnit, out var candidates, out var reason))
                return new ConversionResult { ErrorMessage = ConverterText.Format("{0} Please specify '{1}'.", uiCulture,
                    ConverterText.Get(reason, uiCulture), string.Join($"' {ConverterText.Get("or", uiCulture)} '", candidates)) };
            return new ConversionResult { Success = false, ErrorMessage = ConverterText.Format("Unknown unit: {0}", uiCulture, query.FromUnit) };
        }

        if (!SupportsDimension(module, query.Dimension))
        {
            return new ConversionResult { ErrorMessage = ConverterText.Format("{0} does not support {1} units.", uiCulture,
                ConverterText.Get(module.Category, uiCulture), ConverterText.Get(query.Dimension.ToString().ToLowerInvariant(), uiCulture)) };
        }

        if (!TryConvertWithDimension(module, query.Dimension, query.Value, normalizedUnit, normalizedUnit, out var validated) ||
            !double.IsFinite(validated))
            return new ConversionResult { ErrorMessage = ConverterText.Get("The value is not valid for this unit.", uiCulture) };

        var suggestions = new List<ConversionSuggestion>();
        foreach (var suggestion in module.GetCommonConversions(query.Value, normalizedUnit))
        {
            var converted = suggestion.Output.Value;
            var targetUnit = suggestion.Output.Unit.Unit;
            if (query.Dimension != UnitDimension.Linear &&
                !TryConvertWithDimension(module, query.Dimension, query.Value, normalizedUnit, targetUnit, out converted))
            {
                continue;
            }

            var type = ResolveUnitType(module, query.Dimension);
            var metadata = UnitMetadataProvider.GetMetadata(targetUnit, this._modules, uiCulture);
            var token = new UnitToken { Unit = targetUnit, Type = type, Dimension = query.Dimension, Metadata = metadata };
            suggestions.Add(new ConversionSuggestion(new Quantity(converted, token), this.FormatResult(converted, targetUnit, query.Dimension, culture)));
        }

        var type2 = ResolveUnitType(module, query.Dimension);
        var metadata2 = UnitMetadataProvider.GetMetadata(normalizedUnit, this._modules, uiCulture);
        var token2 = new UnitToken { Unit = normalizedUnit, Type = type2, Dimension = query.Dimension, Metadata = metadata2 };
        return new ConversionResult
        {
            Success = true,
            Output = new Quantity(query.Value, token2),
            FormattedResult = this.FormatResult(query.Value, normalizedUnit, query.Dimension, culture),
            Suggestions = suggestions.ToArray()
        };
    }

    private IConversionModule? FindModuleForUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        normalizedUnit = string.Empty;
        IConversionModule? selectedModule = null;
        var selectedRank = int.MaxValue;

        foreach (var module in this._modules)
        {
            if (module is DescriptorBasedConversionModule desc && desc.TryGetAmbiguity(unit, out var candidates, out var reason))
            {
                normalizedUnit = string.Empty;
                return null;
            }

            if (module.TryNormalizeUnit(unit, culture, out var normalized))
            {
                var metadata = module.GetUnitMetadata(normalized, culture);
                var rank = unit == normalized || unit == metadata?.Symbol ? 0
                    : module.GetSupportedUnits()[normalized].Contains(unit, StringComparer.Ordinal) ||
                      ConverterText.GetUnitNames(module.Category, normalized, culture).Contains(unit, StringComparer.Ordinal) ? 1 : 2;
                if (rank < selectedRank)
                {
                    selectedModule = module;
                    normalizedUnit = normalized;
                    selectedRank = rank;
                }
            }
        }

        return selectedModule;
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
        if (!SupportsDimension(module, dim))
        {
            result = 0;
            return false;
        }

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
        return double.IsFinite(result);
    }

    private static bool SupportsDimension(IConversionModule module, UnitDimension dimension) => dimension switch
    {
        UnitDimension.Squared => module.SupportsSquaredUnits,
        UnitDimension.Cubic => module.SupportsCubicUnits,
        _ => true
    };

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

    public string FormatConversionQuery(string source, string target, CultureInfo culture,
        ConversionWordLanguage mode = ConversionWordLanguage.Default)
        => FormatConversionQuery(source, target, culture, mode, this._uiCulture ?? culture);

    private CultureInfo ResolveQueryCulture(CultureInfo culture, ConversionWordLanguage mode) =>
        (mode == ConversionWordLanguage.Default ? ConverterText.DefaultConversionWordLanguage : mode) ==
        ConversionWordLanguage.InterfaceInputAndEnglish && this._uiCulture != null && this._uiCulture.Name != culture.Name &&
        culture is not ConversionQueryCulture
            ? new ConversionQueryCulture(culture, this._uiCulture) : culture;

    private string FormatConversionQuery(string source, string target, CultureInfo culture, ConversionWordLanguage mode, CultureInfo queryCulture)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentNullException.ThrowIfNull(target);
        culture = ResolveQueryCulture(culture, mode);
        var connector = ConverterText.GetConversionWords(queryCulture, mode)[0];
        if (!ConverterText.GetConversionWords(culture, mode).Contains(connector, StringComparer.OrdinalIgnoreCase))
            return $"{source} → {target}";

        return ConverterText.GetCjkQueryLanguage(queryCulture) switch
        {
            "ja" => source + "を" + (target.Length > 0 ? target + "に" : ""),
            "ko" => source + KoreanParticle(source, false) + (target.Length > 0 ? " " + target + KoreanParticle(target, true) : ""),
            "zh" => source + connector + target,
            _ => $"{source} {connector} {target}"
        };

        string KoreanParticle(string text, bool destination)
        {
            var unit = text;
            if (!destination)
            {
                var parsed = ConversionQueryParser.TryParseQuery(text, culture, this._modules, mode);
                unit = parsed?.Components.LastOrDefault()?.Unit.Unit ?? parsed?.FromUnit ?? text;
            }
            var module = FindModuleForUnit(unit, culture, out var id);
            if (module == null)
            {
                unit = unit.TrimEnd('²', '³').Split(' ', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? unit;
                module = FindModuleForUnit(unit, culture, out id);
            }
            // Symbols and aliases take their pronunciation from the resolved unit name.
            var spoken = (module?.GetUnitMetadata(id, queryCulture)?.DisplayName ?? unit).TrimEnd('²', '³');
            var last = spoken.Length > 0 ? spoken[^1] : '\0';
            var finalConsonant = last is >= '\uac00' and <= '\ud7a3' ? (last - '\uac00') % 28 : 0;
            if (!destination)
                return finalConsonant == 0 ? "를" : "을";
            return finalConsonant is 0 or 8 ? "로" : "으로";
        }
    }

    public string FormatNumber(double value, CultureInfo culture)
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

        return value != 0 && Math.Round(value, precision) == 0
            ? value.ToString($"G{Math.Min(precision + 1, 15)}", culture)
            : TrimZeros(value.ToString($"F{precision}", culture), culture);
    }

    private string FormatResult(double value, string unit, UnitDimension dimension, CultureInfo culture)
    {
        var dimensionSuffix = dimension switch
        {
            UnitDimension.Squared => "²",
            UnitDimension.Cubic => "³",
            _ => ""
        };

        var symbol = UnitMetadataProvider.GetSymbol(unit, this._modules, culture);
        if (unit is "min/km" or "min/mi")
        {
            var minutes = Math.Floor(value);
            var secondsText = FormatNumber((value - minutes) * 60, culture);
            if (!secondsText.Contains('E'))
            {
                var seconds = double.Parse(secondsText, NumberStyles.Float, culture);
                if (seconds >= 60)
                {
                    minutes++;
                    seconds = 0;
                    secondsText = "0";
                }
                if (seconds < 10)
                {
                    secondsText = "0" + secondsText;
                }

                return $"{minutes.ToString("0", culture)}:{secondsText} {symbol}";
            }
        }
        return $"{FormatNumber(value, culture)} {symbol}{dimensionSuffix}";
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
            "TemperatureDelta" => UnitType.TemperatureDelta,
            "Frequency" => UnitType.Frequency,
            "Force" => UnitType.Force,
            "Torque" => UnitType.Torque,
            "Acceleration" => UnitType.Acceleration,
            "Density" => UnitType.Density,
            "Voltage" => UnitType.Voltage,
            "Current" => UnitType.Current,
            "Resistance" => UnitType.Resistance,
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
            "DataRate" => UnitType.DataRate,
            "FuelEconomy" => UnitType.FuelEconomy,
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