// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.RegularExpressions;

namespace JPSoftworks.UnitConverter.Core;

public class ConversionQueryParser
{
    private static readonly Dictionary<string, UnitDimension> DimensionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Squared indicators
        ["square"] = UnitDimension.Squared,
        ["sq"] = UnitDimension.Squared,
        ["²"] = UnitDimension.Squared,
        ["^2"] = UnitDimension.Squared,
        ["2"] = UnitDimension.Squared, // for patterns like "cm2"

        // Cubic indicators
        ["cubic"] = UnitDimension.Cubic,
        ["cu"] = UnitDimension.Cubic,
        ["³"] = UnitDimension.Cubic,
        ["^3"] = UnitDimension.Cubic,
        ["3"] = UnitDimension.Cubic // for patterns like "cm3"
    };

    // Note: Do NOT include "in" here to avoid conflict with inches unit
    private static readonly string[] ConversionIndicators = ["to", "as", "->", "=", "=>", "→"];

    public ParsedConversionQuery? TryParseQuery(string query, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        query = query.Trim();

        // Try explicit conversion first (e.g., "1 m to cm", "1m->cm", "10°C as F")
        var explicitResult = this.TryParseExplicitConversion(query, culture);
        if (explicitResult != null)
        {
            return explicitResult;
        }

        // Try implicit conversion (e.g., "60 mins", "100 cm", "5 cubic feet")
        var implicitResult = this.TryParseImplicitConversion(query, culture);
        if (implicitResult != null)
        {
            return implicitResult;
        }

        return null;
    }

    private ParsedConversionQuery? TryParseExplicitConversion(string query, CultureInfo culture)
    {
        // Try to find conversion indicators as whole words or symbols, not as substrings
        var conversionIndicatorPattern = @"\b(?:to|as)\b|(?:->|=|=>|→)";
        var match = Regex.Match(query, conversionIndicatorPattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return null;
        }

        var indicator = match.Value.Trim();
        var indicatorIndex = match.Index;

        // Split the query at the conversion indicator
        var leftPart = query[..indicatorIndex].Trim();
        var rightPart = query[(indicatorIndex + indicator.Length)..].Trim();

        if (string.IsNullOrWhiteSpace(leftPart) || string.IsNullOrWhiteSpace(rightPart))
        {
            return null;
        }

        // Parse the left part as "value + fromUnit"
        var valueUnitPattern = @"^(?<value>[\d\.,\-\+\s]+)\s*(?<unit>.+)$";
        var leftMatch = Regex.Match(leftPart, valueUnitPattern);

        if (!leftMatch.Success)
        {
            return null;
        }

        var valueStr = leftMatch.Groups["value"].Value.Trim();
        var fromUnitStr = leftMatch.Groups["unit"].Value.Trim();
        var toUnitStr = rightPart;

        if (!TryParseValue(valueStr, culture, out var value))
        {
            return null;
        }

        // Reject cases with empty units or units that are just connectors
        if (string.IsNullOrWhiteSpace(fromUnitStr) || IsConnector(fromUnitStr) ||
            string.IsNullOrWhiteSpace(toUnitStr) || IsConnector(toUnitStr))
        {
            return null;
        }

        var (fromUnit, fromDimension) = this.ParseUnitWithDimension(fromUnitStr);
        var (toUnit, toDimension) = this.ParseUnitWithDimension(toUnitStr);

        // Use the dimension from either unit (only one needs to be specified for squared/cubic)
        var dimension = fromDimension != UnitDimension.Linear ? fromDimension : toDimension;

        return new ParsedConversionQuery
        {
            Value = value,
            FromUnit = fromUnit,
            ToUnit = toUnit,
            Dimension = dimension,
            IsImplicitConversion = false,
            OriginalQuery = query
        };
    }

    private ParsedConversionQuery? TryParseImplicitConversion(string query, CultureInfo culture)
    {
        // Allow multi-word unit tail, e.g., "15 square meters", "5 cubic feet"
        var implicitPattern = @"^(?<value>[\d\.,\-\+\s]+)\s*(?<unit>.+)$";
        var match = Regex.Match(query, implicitPattern, RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            return null;
        }

        var valueStr = match.Groups["value"].Value.Trim();
        var unitStr = match.Groups["unit"].Value.Trim();

        // Reject when the trailing token is just a connector like "to"
        if (IsConnector(unitStr))
        {
            return null;
        }

        if (!TryParseValue(valueStr, culture, out var value))
        {
            return null;
        }

        var (unit, dimension) = this.ParseUnitWithDimension(unitStr);

        // Extra guard: if unit parsed to empty, reject
        if (string.IsNullOrWhiteSpace(unit))
        {
            return null;
        }

        return new ParsedConversionQuery
        {
            Value = value,
            FromUnit = unit,
            ToUnit = string.Empty,
            Dimension = dimension,
            IsImplicitConversion = true,
            OriginalQuery = query
        };
    }

    private (string unit, UnitDimension dimension) ParseUnitWithDimension(string unitStr)
    {
        // Handle temperature symbols specially (°C, °F)
        if (unitStr.Contains('°'))
        {
            return (unitStr, UnitDimension.Linear);
        }

        // Check for dimension indicators
        var dimension = UnitDimension.Linear;
        var unit = unitStr;

        // Check for superscript numbers or symbols
        if (unitStr.EndsWith("²") || unitStr.EndsWith("^2"))
        {
            dimension = UnitDimension.Squared;
            unit = unitStr.Replace("²", "").Replace("^2", "");
        }
        else if (unitStr.EndsWith("³") || unitStr.EndsWith("^3"))
        {
            dimension = UnitDimension.Cubic;
            unit = unitStr.Replace("³", "").Replace("^3", "");
        }
        else if (Regex.IsMatch(unitStr, @"\d$"))
        {
            // Check for patterns like "cm2" or "m3"
            var numberMatch = Regex.Match(unitStr, @"(?<unit>.+?)(?<number>\d)$");
            if (numberMatch.Success)
            {
                var number = numberMatch.Groups["number"].Value;
                if (number == "2")
                {
                    dimension = UnitDimension.Squared;
                    unit = numberMatch.Groups["unit"].Value;
                }
                else if (number == "3")
                {
                    dimension = UnitDimension.Cubic;
                    unit = numberMatch.Groups["unit"].Value;
                }
            }
        }
        else
        {
            // Check for word-based dimension indicators (square, cubic, sq, cu)
            var words = unitStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (words.Length >= 2)
            {
                var firstWord = words[0];
                if (DimensionKeywords.TryGetValue(firstWord, out var dim))
                {
                    dimension = dim;
                    unit = string.Join(" ", words.Skip(1));
                }
            }
        }

        return (unit.Trim(), dimension);
    }

    private static bool TryParseValue(string valueStr, CultureInfo culture, out double value)
    {
        value = 0;

        // Normalize number: remove spaces, NBSP, and underscores used as thousands separators
        valueStr = valueStr.Replace(" °", "°").Replace(" ", string.Empty).Replace("\u00A0", string.Empty)
            .Replace("_", string.Empty).Trim();

        // Try parsing with the specified culture
        if (double.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowThousands, culture, out value))
        {
            return true;
        }

        // Try parsing with invariant culture as fallback
        if (double.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture,
                out value))
        {
            return true;
        }

        // Try parsing with current culture as last resort
        return double.TryParse(valueStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture,
            out value);
    }

    private static bool IsConnector(string text)
    {
        var t = text.Trim();
        // If the text is exactly a connector keyword, treat as connector
        return ConversionIndicators.Any(ind => string.Equals(t, ind, StringComparison.OrdinalIgnoreCase));
    }
}