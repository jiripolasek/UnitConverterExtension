// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core;

/// <summary>
///     Provides unit metadata for display names, descriptions, and other localized information.
///     Acts as both a fallback provider and aggregator for module-based metadata.
/// </summary>
public static class UnitMetadataProvider
{
    private static readonly Dictionary<string, UnitMetadata> _fallbackMetadata = new(StringComparer.OrdinalIgnoreCase)
    {
    };

    /// <summary>
    ///     Gets metadata for a specific unit, trying modules first, then fallback
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="modules">Available conversion modules (optional)</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Unit metadata or null if not found</returns>
    public static UnitMetadata? GetMetadata(string unit, IEnumerable<IConversionModule>? modules = null, CultureInfo? culture = null)
    {
        // Try modules first (preferred for localization)
        if (modules != null)
        {
            foreach (var module in modules)
            {
                if (module.GetSupportedUnits().ContainsKey(unit) && module.GetUnitMetadata(unit, culture) is { } metadata)
                    return metadata;
            }
            foreach (var module in modules)
            {
                var metadata = module.GetUnitMetadata(unit, culture);
                if (metadata != null)
                    return metadata;
            }
        }

        // Fall back to static metadata
        return _fallbackMetadata.TryGetValue(unit, out var fallback) ? fallback : null;
    }

    /// <summary>
    ///     Gets metadata for a specific unit using fallback data only
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <returns>Unit metadata or null if not found</returns>
    public static UnitMetadata? GetMetadata(string unit)
    {
        return _fallbackMetadata.TryGetValue(unit, out var metadata) ? metadata : null;
    }

    /// <summary>
    ///     Gets all available metadata from modules and fallback
    /// </summary>
    /// <param name="modules">Available conversion modules (optional)</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Combined metadata from all sources</returns>
    public static IReadOnlyDictionary<string, UnitMetadata> GetAllMetadata(IEnumerable<IConversionModule>? modules = null, CultureInfo? culture = null)
    {
        var result = new Dictionary<string, UnitMetadata>(_fallbackMetadata, StringComparer.OrdinalIgnoreCase);

        // Override with module metadata (preferred)
        if (modules != null)
        {
            foreach (var module in modules)
            {
                foreach (var kvp in module.GetAllUnitMetadata(culture))
                {
                    result[kvp.Key] = kvp.Value;
                }
            }
        }

        return result.AsReadOnly();
    }

    /// <summary>
    ///     Gets display name for a unit, trying modules first, then fallback
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="modules">Available conversion modules (optional)</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Display name or unit identifier</returns>
    public static string GetDisplayName(string unit, IEnumerable<IConversionModule>? modules = null, CultureInfo? culture = null)
    {
        return GetMetadata(unit, modules, culture)?.DisplayName ?? unit;
    }

    /// <summary>
    ///     Gets symbol for a unit, trying modules first, then fallback
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="modules">Available conversion modules (optional)</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Symbol or unit identifier</returns>
    public static string GetSymbol(string unit, IEnumerable<IConversionModule>? modules = null, CultureInfo? culture = null)
    {
        return GetMetadata(unit, modules, culture)?.Symbol ?? unit;
    }

    /// <summary>
    ///     Checks if a unit is commonly used
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="modules">Available conversion modules (optional)</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>True if common, false otherwise (defaults to true if metadata not found)</returns>
    public static bool IsCommonUnit(string unit, IEnumerable<IConversionModule>? modules = null, CultureInfo? culture = null)
    {
        return GetMetadata(unit, modules, culture)?.IsCommon ?? true;
    }

    // Keep existing methods for backward compatibility
    public static IEnumerable<(string Unit, UnitMetadata Metadata)> GetUnitsBySystem(string system)
    {
        return _fallbackMetadata.Where(kvp => string.Equals(kvp.Value.System, system, StringComparison.OrdinalIgnoreCase))
                               .Select(kvp => (kvp.Key, kvp.Value));
    }

    public static IEnumerable<(string Unit, UnitMetadata Metadata)> GetUnitsByTag(string tag)
    {
        return _fallbackMetadata.Where(kvp => kvp.Value.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                               .Select(kvp => (kvp.Key, kvp.Value));
    }

    public static void SetMetadata(string unit, UnitMetadata metadata)
    {
        _fallbackMetadata[unit] = metadata;
    }
}
