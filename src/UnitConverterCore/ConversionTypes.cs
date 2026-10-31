// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace JPSoftworks.UnitConverter.Core;

/// <summary>
///     Represents a parsed conversion query
/// </summary>
public record ParsedConversionQuery
{
    public double Value { get; init; }
    public string FromUnit { get; init; } = string.Empty;
    public string ToUnit { get; init; } = string.Empty;
    public UnitDimension Dimension { get; init; } = UnitDimension.Linear;
    public UnitDimension FromDimension { get; init; } = UnitDimension.Linear;
    public UnitDimension ToDimension { get; init; } = UnitDimension.Linear;
    public Quantity[] Components { get; init; } = [];
    public bool IsImplicitConversion { get; init; }
    public string OriginalQuery { get; init; } = string.Empty;
}

/// <summary>
///     Represents the dimension of a unit
/// </summary>
public enum UnitDimension
{
    Linear,
    Squared,
    Cubic
}

/// <summary>
///     High-level unit type for UI (used to choose icons)
/// </summary>
public enum UnitType
{
    Unknown,
    Length,
    Area,
    Volume,
    Speed,
    Time,
    Power,
    Temperature,
    Mass,
    Energy,
    Data,
    Pressure,
    Angle,
    DataRate,
    FuelEconomy,
    TemperatureDelta,
    Frequency,
    Force,
    Torque,
    Acceleration,
    Density,
    Voltage,
    Current,
    Resistance
}

/// <summary>
///     Contains metadata about a unit for enhanced display and localization
/// </summary>
public record UnitMetadata
{
    /// <summary>
    ///     Localizable display name for the unit
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    ///     Short description of the unit (optional)
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    ///     Symbol used for the unit (may be different from the unit identifier)
    /// </summary>
    public string Symbol { get; init; } = string.Empty;

    /// <summary>
    ///     Whether this unit is commonly used in everyday contexts
    /// </summary>
    public bool IsCommon { get; init; } = true;

    /// <summary>
    ///     The system this unit belongs to (Metric, Imperial, US, etc.)
    /// </summary>
    public string System { get; init; } = string.Empty;

    /// <summary>
    ///     Additional tags for categorization or filtering
    /// </summary>
    public string[] Tags { get; init; } = [];

    /// <summary>
    ///     Extensible properties for future metadata needs
    /// </summary>
    public IReadOnlyDictionary<string, object> Properties { get; init; } =
        new Dictionary<string, object>();
}

/// <summary>
///     Wraps a unit symbol together with its inferred type, dimension, and metadata
/// </summary>
public record UnitToken
{
    public string Unit { get; init; } = string.Empty;
    public UnitType Type { get; init; } = UnitType.Unknown;
    public UnitDimension Dimension { get; init; } = UnitDimension.Linear;

    /// <summary>
    ///     Enhanced metadata for the unit (optional)
    /// </summary>
    public UnitMetadata? Metadata { get; init; }
}

public record Quantity
{
    public Quantity()
    {
    }

    [SetsRequiredMembers]
    public Quantity(double value, UnitToken unit)
    {
        this.Value = value;
        this.Unit = unit;
    }

    public required double Value { get; init; }

    public required UnitToken Unit { get; init; } = new();

    public override string ToString()
    {
        return $"{this.Value} {this.Unit.Unit}";
    }
}

/// <summary>
///     Represents the result of a conversion operation
/// </summary>
public record ConversionResult
{
    public bool Success { get; init; }
    public bool IsIncomplete { get; init; }
    public QueryCompletion[] Completions { get; init; } = [];
    public QueryCompletion[] OtherInterpretations { get; init; } = [];

    public Quantity Output { get; init; } = new(0, new UnitToken());

    public Quantity[] OutputComponents { get; init; } = [];

    public string FormattedResult { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public ConversionSuggestion[] Suggestions { get; init; } = [];
}

public record QueryCompletion(string Query, UnitToken Unit)
{
    public string FormattedResult { get; init; } = string.Empty;
}