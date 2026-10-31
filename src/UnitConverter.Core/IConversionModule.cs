// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core;

/// <summary>
///     Represents a conversion module for a specific category of units (e.g., temperature, length)
/// </summary>
public interface IConversionModule
{
    /// <summary>
    ///     The category name of this module (e.g., "Temperature", "Length")
    /// </summary>
    string Category { get; }

    /// <summary>
    ///     Checks if this module supports squared units (e.g., area calculations)
    /// </summary>
    bool SupportsSquaredUnits { get; }

    /// <summary>
    ///     Checks if this module supports cubic units (e.g., volume calculations)
    /// </summary>
    bool SupportsCubicUnits { get; }

    /// <summary>
    ///     Gets all supported unit names and their aliases for this module
    /// </summary>
    IReadOnlyDictionary<string, string[]> GetSupportedUnits();

    /// <summary>
    ///     Gets metadata for a specific unit within this module
    /// </summary>
    /// <param name="unit">Unit identifier</param>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Unit metadata or null if unit not supported by this module</returns>
    UnitMetadata? GetUnitMetadata(string unit, CultureInfo? culture = null);

    /// <summary>
    ///     Gets metadata for all units supported by this module
    /// </summary>
    /// <param name="culture">Culture for localization (optional)</param>
    /// <returns>Dictionary of unit metadata</returns>
    IReadOnlyDictionary<string, UnitMetadata> GetAllUnitMetadata(CultureInfo? culture = null);

    /// <summary>
    ///     Attempts to normalize a unit string to a standard unit name
    /// </summary>
    bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit);

    /// <summary>
    ///     Converts a value from one unit to another within this module
    /// </summary>
    bool TryConvert(double value, string fromUnit, string toUnit, out double result);

    /// <summary>
    ///     Gets common conversions for a given unit and value
    /// </summary>
    IEnumerable<ConversionSuggestion> GetCommonConversions(double value, string unit);
}

/// <summary>
///     Represents a suggested conversion
/// </summary>
public record ConversionSuggestion(Quantity Output, string FormattedResult);