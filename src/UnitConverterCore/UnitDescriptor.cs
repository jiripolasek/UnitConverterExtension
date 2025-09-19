// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core;

/// <summary>
///     Comprehensive descriptor for a unit containing all its properties, metadata, and relationships
/// </summary>
public record UnitDescriptor
{
    /// <summary>
    ///     Primary identifier/symbol for the unit (e.g., "m", "ft", "°C")
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    ///     Display symbol for the unit (may differ from Id, e.g., Id="deg" but Symbol="°")
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    ///     Localizable display name for the unit
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     Alternative names/aliases that can be used to identify this unit
    /// </summary>
    public string[] Aliases { get; init; } = [];

    /// <summary>
    ///     Unit system classification (Metric, Imperial, US, etc.)
    /// </summary>
    public string System { get; init; } = string.Empty;

    /// <summary>
    ///     Whether this unit is commonly used in everyday contexts
    /// </summary>
    public bool IsCommon { get; init; } = true;

    /// <summary>
    ///     Tags for categorization, filtering, or special handling
    /// </summary>
    public string[] Tags { get; init; } = [];

    /// <summary>
    ///     Short description of the unit (optional)
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    ///     Units that are commonly converted to from this unit
    /// </summary>
    public string[] CommonTargets { get; init; } = [];

    /// <summary>
    ///     Priority order for suggestions (lower values = higher priority)
    /// </summary>
    public int SuggestionPriority { get; init; } = 100;

    /// <summary>
    ///     Extensible properties for module-specific or future metadata needs
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } = 
        new Dictionary<string, object>();

    /// <summary>
    ///     Converts this descriptor to UnitMetadata
    /// </summary>
    public UnitMetadata ToMetadata()
    {
        return new UnitMetadata
        {
            DisplayName = string.IsNullOrEmpty(DisplayName) ? Id : DisplayName,
            Symbol = string.IsNullOrEmpty(Symbol) ? Id : Symbol,
            System = System,
            IsCommon = IsCommon,
            Tags = Tags,
            Description = Description,
            Properties = Properties
        };
    }

    /// <summary>
    ///     Gets the effective symbol to display (Symbol if set, otherwise Id)
    /// </summary>
    public string GetEffectiveSymbol() => string.IsNullOrEmpty(Symbol) ? Id : Symbol;

    /// <summary>
    ///     Gets the effective display name (DisplayName if set, otherwise Id)
    /// </summary>
    public string GetEffectiveDisplayName() => string.IsNullOrEmpty(DisplayName) ? Id : DisplayName;

    /// <summary>
    ///     Checks if the given input matches this unit (Id, Symbol, or any alias)
    /// </summary>
    public bool Matches(string input, StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        // Check primary Id
        if (string.Equals(Id, input, comparison))
            return true;

        // Check symbol if different from Id
        if (!string.IsNullOrEmpty(Symbol) && !string.Equals(Symbol, Id, comparison) && 
            string.Equals(Symbol, input, comparison))
            return true;

        // Check aliases
        return Aliases.Any(alias => string.Equals(alias, input, comparison));
    }

    /// <summary>
    ///     Gets common target units filtered by relevance for the given value
    /// </summary>
    public string[] GetRelevantTargets(double value)
    {
        // Default implementation - derived classes or configuration can override this logic
        return CommonTargets;
    }
}

/// <summary>
///     Collection of unit descriptors for a conversion module with helper methods
/// </summary>
public class UnitDescriptorCollection
{
    // Case-sensitive for Ids and Symbols to distinguish "kb" vs "kB".
    private readonly Dictionary<string, UnitDescriptor> _units;
    private readonly Dictionary<string, UnitDescriptor> _symbolLookup;
    // Case-insensitive for aliases like words ("kilobyte", "megabit").
    private readonly Dictionary<string, UnitDescriptor> _aliasLookup;

    public UnitDescriptorCollection(IEnumerable<UnitDescriptor> units)
    {
        _units = new Dictionary<string, UnitDescriptor>(StringComparer.Ordinal);
        _symbolLookup = new Dictionary<string, UnitDescriptor>(StringComparer.Ordinal);
        _aliasLookup = new Dictionary<string, UnitDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var unit in units)
        {
            // Add by Id (case-sensitive)
            _units[unit.Id] = unit;
            
            // Add aliases to case-insensitive lookup
            foreach (var alias in unit.Aliases)
            {
                _aliasLookup[alias] = unit;
            }

            // Add symbol mapping if different from Id (case-sensitive)
            if (!string.IsNullOrEmpty(unit.Symbol) && 
                !string.Equals(unit.Symbol, unit.Id, StringComparison.Ordinal))
            {
                _symbolLookup[unit.Symbol] = unit;
            }
        }
    }

    /// <summary>
    ///     Gets all unit descriptors
    /// </summary>
    public IEnumerable<UnitDescriptor> GetAll() => _units.Values;

    /// <summary>
    ///     Gets a unit descriptor by its Id
    /// </summary>
    public UnitDescriptor? GetById(string id) => 
        _units.TryGetValue(id, out var unit) ? unit : null;

    /// <summary>
    ///     Finds a unit descriptor by input (Id, symbol, or alias)
    ///     - Id and symbol are matched case-sensitively to distinguish units like "kb" vs "kB".
    ///     - Aliases are matched case-insensitively for user convenience.
    /// </summary>
    public UnitDescriptor? FindByInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Try direct Id (case-sensitive)
        if (_units.TryGetValue(input, out var unit))
            return unit;

        // Try symbol (case-sensitive)
        if (_symbolLookup.TryGetValue(input, out unit))
            return unit;

        // Try alias (case-insensitive)
        if (_aliasLookup.TryGetValue(input, out unit))
            return unit;

        return null;
    }

    /// <summary>
    ///     Tries to normalize an input to a canonical unit Id
    /// </summary>
    public bool TryNormalize(string input, out string normalizedId, out UnitDescriptor? descriptor)
    {
        descriptor = FindByInput(input);
        normalizedId = descriptor?.Id ?? string.Empty;
        return descriptor != null;
    }

    /// <summary>
    ///     Case-insensitive normalization across Id, Symbol, and Aliases.
    ///     Useful for modules that want to be lenient about casing.
    /// </summary>
    public bool TryNormalizeCaseInsensitive(string input, out string normalizedId, out UnitDescriptor? descriptor)
    {
        normalizedId = string.Empty;
        descriptor = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var trimmed = input.Trim();

        // Quick alias lookup first (already case-insensitive)
        if (_aliasLookup.TryGetValue(trimmed, out var byAlias))
        {
            descriptor = byAlias;
            normalizedId = byAlias.Id;
            return true;
        }

        // Id and Symbol via case-insensitive scan (safe in small sets)
        var match = _units.Values.FirstOrDefault(d =>
            string.Equals(d.Id, trimmed, StringComparison.OrdinalIgnoreCase) ||
            (!string.IsNullOrEmpty(d.Symbol) && string.Equals(d.Symbol, trimmed, StringComparison.OrdinalIgnoreCase))
        );
        if (match != null)
        {
            descriptor = match;
            normalizedId = match.Id;
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Gets all supported units as dictionary (for IConversionModule compatibility)
    /// </summary>
    public IReadOnlyDictionary<string, string[]> GetSupportedUnits()
    {
        return _units.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Aliases,
            StringComparer.Ordinal
        ).AsReadOnly();
    }

    /// <summary>
    ///     Gets all unit metadata
    /// </summary>
    public IReadOnlyDictionary<string, UnitMetadata> GetAllMetadata()
    {
        return _units.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.ToMetadata(),
            StringComparer.Ordinal
        ).AsReadOnly();
    }

    /// <summary>
    ///     Gets metadata for a specific unit
    /// </summary>
    public UnitMetadata? GetMetadata(string unitId)
    {
        return GetById(unitId)?.ToMetadata();
    }

    /// <summary>
    ///     Gets units filtered by system
    /// </summary>
    public IEnumerable<UnitDescriptor> GetBySystem(string system)
    {
        return _units.Values.Where(u => 
            string.Equals(u.System, system, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets units filtered by tag
    /// </summary>
    public IEnumerable<UnitDescriptor> GetByTag(string tag)
    {
        return _units.Values.Where(u => 
            u.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Gets common units (IsCommon = true)
    /// </summary>
    public IEnumerable<UnitDescriptor> GetCommonUnits()
    {
        return _units.Values.Where(u => u.IsCommon);
    }
}