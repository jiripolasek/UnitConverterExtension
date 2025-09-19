// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Base class for conversion modules that provides default metadata implementation
/// </summary>
public abstract class ConversionModuleBase : IConversionModule
{
    public abstract string Category { get; }
    public abstract bool SupportsSquaredUnits { get; }
    public abstract bool SupportsCubicUnits { get; }
    public virtual bool IsCaseSensitive => false;
    public abstract IReadOnlyDictionary<string, string[]> GetSupportedUnits();
    public abstract bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit);
    public abstract bool TryConvert(double value, string fromUnit, string toUnit, out double result);
    public abstract IEnumerable<ConversionSuggestion> GetCommonConversions(double value, string unit);

    /// <summary>
    ///     Gets metadata for a specific unit within this module.
    ///     Default implementation delegates to UnitMetadataProvider fallback.
    ///     Override in derived classes to provide localized metadata.
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Unit metadata or null if unit not supported by this module</returns>
    public virtual UnitMetadata? GetUnitMetadata(string unit, CultureInfo? culture = null)
    {
        // First check if this module supports the unit
        if (!TryNormalizeUnit(unit, culture ?? CultureInfo.InvariantCulture, out var normalizedUnit))
            return null;

        // Delegate to fallback provider for now
        // Derived classes can override this method to provide localized metadata
        return UnitMetadataProvider.GetMetadata(normalizedUnit);
    }

    /// <summary>
    ///     Gets metadata for all units supported by this module.
    ///     Default implementation uses GetUnitMetadata for each supported unit.
    /// </summary>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Dictionary of unit metadata</returns>
    public virtual IReadOnlyDictionary<string, UnitMetadata> GetAllUnitMetadata(CultureInfo? culture = null)
    {
        var result = new Dictionary<string, UnitMetadata>(StringComparer.OrdinalIgnoreCase);
        
        foreach (var unit in GetSupportedUnits().Keys)
        {
            var metadata = GetUnitMetadata(unit, culture);
            if (metadata != null)
            {
                result[unit] = metadata;
            }
        }

        return result.AsReadOnly();
    }

    /// <summary>
    ///     Creates enhanced UnitToken with metadata for this module
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="type">Unit type</param>
    /// <param name="dimension">Unit dimension</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>UnitToken with metadata</returns>
    protected UnitToken CreateUnitToken(string unit, UnitType type, UnitDimension dimension, CultureInfo? culture = null)
    {
        var metadata = GetUnitMetadata(unit, culture);
        return new UnitToken 
        { 
            Unit = unit, 
            Type = type, 
            Dimension = dimension, 
            Metadata = metadata 
        };
    }
}

/// <summary>
///     Enhanced base class for conversion modules using the UnitDescriptor system
/// </summary>
public abstract class DescriptorBasedConversionModule : IConversionModule
{
    protected readonly UnitDescriptorCollection Units;

    // Optional ambiguity provider for this module
    protected virtual IAmbiguityProvider? AmbiguityProvider => null;

    // Case-sensitivity toggle (defaults to insensitive)
    public virtual bool IsCaseSensitive => false;

    // Expose a safe public check for ambiguity without exposing the provider
    public bool TryGetAmbiguity(string token, out string[] candidates, out string reasonMessage)
    {
        if (AmbiguityProvider != null && AmbiguityProvider.TryGetAmbiguity(token, out candidates!, out reasonMessage!))
        {
            return true;
        }
        candidates = [];
        reasonMessage = string.Empty;
        return false;
    }

    // Expose enumeration of ambiguous tokens for testing/validation
    public IEnumerable<string> GetAmbiguousTokens()
    {
        return AmbiguityProvider?.GetAmbiguousTokens() ?? Enumerable.Empty<string>();
    }

    protected DescriptorBasedConversionModule(IEnumerable<UnitDescriptor> unitDescriptors)
    {
        Units = new UnitDescriptorCollection(unitDescriptors);
    }

    public abstract string Category { get; }
    public abstract bool SupportsSquaredUnits { get; }
    public abstract bool SupportsCubicUnits { get; }

    public virtual IReadOnlyDictionary<string, string[]> GetSupportedUnits()
    {
        return Units.GetSupportedUnits();
    }

    public virtual bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        // Disallow known ambiguous aliases for this module
        if (TryGetAmbiguity(unit, out var _, out var _))
        {
            normalizedUnit = string.Empty;
            return false;
        }

        if (IsCaseSensitive)
        {
            return Units.TryNormalize(unit, out normalizedUnit, out _);
        }

        return Units.TryNormalizeCaseInsensitive(unit, out normalizedUnit, out _);
    }

    public virtual UnitMetadata? GetUnitMetadata(string unit, CultureInfo? culture = null)
    {
        if (!TryNormalizeUnit(unit, culture ?? CultureInfo.InvariantCulture, out var normalizedUnit))
            return null;

        return Units.GetMetadata(normalizedUnit);
    }

    public virtual IReadOnlyDictionary<string, UnitMetadata> GetAllUnitMetadata(CultureInfo? culture = null)
    {
        return Units.GetAllMetadata();
    }

    public abstract bool TryConvert(double value, string fromUnit, string toUnit, out double result);

    public virtual IEnumerable<ConversionSuggestion> GetCommonConversions(double value, string unit)
    {
        if (!TryNormalizeUnit(unit, CultureInfo.InvariantCulture, out var normalizedUnit))
            yield break;

        var unitDescriptor = Units.GetById(normalizedUnit);
        if (unitDescriptor == null)
            yield break;

        var suggestions = new List<ConversionSuggestion>();
        var targets = unitDescriptor.GetRelevantTargets(value);

        foreach (var targetUnit in targets)
        {
            if (TryConvert(value, normalizedUnit, targetUnit, out var convertedValue))
            {
                var token = CreateUnitToken(targetUnit, GetUnitType(), UnitDimension.Linear);
                var formatted = FormatValue(convertedValue, targetUnit);
                suggestions.Add(new ConversionSuggestion(new Quantity(convertedValue, token), formatted));
            }
        }

        // Sort by relevance and take top suggestions
        var orderedSuggestions = suggestions
            .Where(s => IsReasonableValue(s.Output.Value))
            .OrderBy(s => GetRelevanceScore(s.Output.Value, s.Output.Unit.Unit))
            .Take(5);

        foreach (var suggestion in orderedSuggestions)
        {
            yield return suggestion;
        }
    }

    /// <summary>
    ///     Creates enhanced UnitToken with metadata
    /// </summary>
    protected UnitToken CreateUnitToken(string unit, UnitType type, UnitDimension dimension)
    {
        var metadata = GetUnitMetadata(unit);
        return new UnitToken 
        { 
            Unit = unit, 
            Type = type, 
            Dimension = dimension, 
            Metadata = metadata 
        };
    }

    /// <summary>
    ///     Gets the primary unit type for this module
    /// </summary>
    protected abstract UnitType GetUnitType();

    /// <summary>
    ///     Formats a value with appropriate precision for display
    /// </summary>
    protected virtual string FormatValue(double value, string unit)
    {
        var precision = value switch
        {
            < 0.01 => 4,
            < 0.1 => 3,
            < 1 => 2,
            < 10 => 2,
            < 1000 => 1,
            _ => 0
        };

        var unitDescriptor = Units.GetById(unit);
        var displaySymbol = unitDescriptor?.GetEffectiveSymbol() ?? unit;
        
        var number = value.ToString($"F{precision}", CultureInfo.CurrentCulture);
        number = TrimZeros(number, CultureInfo.CurrentCulture);
        return $"{number} {displaySymbol}";
    }

    protected static string TrimZeros(string s, CultureInfo culture)
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

    /// <summary>
    ///     Determines if a value is reasonable for display suggestions
    /// </summary>
    protected virtual bool IsReasonableValue(double value)
    {
        return value > 0.001 && value < 1_000_000 && !double.IsInfinity(value) && !double.IsNaN(value);
    }

    /// <summary>
    ///     Calculates relevance score for suggestions (lower = more relevant)
    /// </summary>
    protected virtual double GetRelevanceScore(double value, string unit)
    {
        var unitDescriptor = Units.GetById(unit);
        
        // Base score on value range (prefer values between 0.1 and 1000)
        var valueScore = value switch
        {
            >= 0.1 and <= 1000 => 0,
            > 0.01 and < 0.1 => 1,
            > 1000 and <= 10000 => 1,
            _ => 2
        };

        // Factor in unit priority if available
        var priorityScore = (unitDescriptor?.SuggestionPriority ?? 100) / 100.0;

        // Factor in commonality
        var commonScore = (unitDescriptor?.IsCommon ?? true) ? 0 : 1;

        return valueScore + priorityScore + commonScore;
    }
}