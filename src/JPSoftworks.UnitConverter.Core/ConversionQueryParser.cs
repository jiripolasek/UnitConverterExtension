using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace JPSoftworks.UnitConverter.Core;

public sealed class ConversionQueryParser
{
    private static readonly string[] Connectors = ["to", "->", "as", "→", "=>"];

    // Matches superscripts in Unicode and normal digits
    private static readonly char Sup2 = '²';
    private static readonly char Sup3 = '³';

    public ParsedConversionQuery? TryParseQuery(string query, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(query))
            return null;

        var original = query;
        query = NormalizeWhitespace(query);

        // Detect connector (explicit conversion)
        var (left, connector, right) = SplitByConnector(query);

        if (connector is null)
        {
            // Implicit: "<value> [square|sq|cubic|cu] <unit[2|³]?>"
            if (!TryParseLeftSide(left, culture, out var value, out var fromUnit, out var dimension))
                return null;

            // Reject invalid where the "fromUnit" is actually a connector
            if (IsConnector(fromUnit))
                return null;

            return new ParsedConversionQuery
            {
                Value = value,
                FromUnit = fromUnit,
                ToUnit = string.Empty,
                Dimension = dimension,
                IsImplicitConversion = true,
                OriginalQuery = original
            };
        }

        // Explicit: "<value> [square|...|cubic|...] <fromUnit> <connector> [square|...|cubic|...] <toUnit>"
        if (!TryParseLeftSide(left, culture, out var leftValue, out var leftFromUnit, out var leftDim))
            return null;

        // Reject invalid where the "fromUnit" is actually a connector
        if (IsConnector(leftFromUnit))
            return null;

        if (!TryParseRightSide(right, out var toUnit, out var rightDim))
            return null;

        // If either side specified dimension, prefer explicit; otherwise default to Linear
        var dimensionFinal = leftDim != UnitDimension.Linear ? leftDim : rightDim;

        return new ParsedConversionQuery
        {
            Value = leftValue,
            FromUnit = leftFromUnit,
            ToUnit = toUnit,
            Dimension = dimensionFinal,
            IsImplicitConversion = false,
            OriginalQuery = original
        };
    }

    private static (string left, string? connector, string right) SplitByConnector(string query)
    {
        // Search for connectors surrounded by whitespace
        // Note: keep order so " -> " does not get split into " -"
        foreach (var c in Connectors)
        {
            var pattern = $" {Regex.Escape(c)} ";
            var idx = CultureInfo.InvariantCulture.CompareInfo.IndexOf(query, pattern, CompareOptions.IgnoreCase);
            if (idx >= 0)
            {
                var left = query[..idx].Trim();
                var right = query[(idx + pattern.Length)..].Trim();
                return (left, c, right);
            }
        }

        // Also try with connectors as separate tokens (e.g. trailing)
        foreach (var c in Connectors)
        {
            var tokens = Tokenize(query);
            var pos = tokens.FindIndex(t => t.Equals(c, StringComparison.OrdinalIgnoreCase));
            if (pos > 0 && pos < tokens.Count - 1)
            {
                var left = string.Join(' ', tokens.Take(pos));
                var right = string.Join(' ', tokens.Skip(pos + 1));
                return (left, c, right);
            }
        }

        return (query, null, string.Empty);
    }

    private static bool TryParseLeftSide(string left, CultureInfo culture, out double value, out string fromUnit, out UnitDimension dimension)
    {
        value = 0;
        fromUnit = string.Empty;
        dimension = UnitDimension.Linear;

        var tokens = Tokenize(left);
        if (tokens.Count == 0)
            return false;

        if (!TryParseNumber(tokens, culture, out value, out var consumed))
            return false;

        var idx = consumed;

        // Optional dimension prefix
        if (idx < tokens.Count && TryParseDimensionPrefix(tokens[idx], out var dimPrefix))
        {
            dimension = dimPrefix;
            idx++;
        }

        if (idx >= tokens.Count)
            return false;

        // Try to read unit token(s)
        var unitToken = tokens[idx];

        // Multi-word units (limited handling; cover common cases if exist)
        if (idx + 1 < tokens.Count)
        {
            var two = $"{tokens[idx]} {tokens[idx + 1]}";
            // Keep generic; modules will normalize multi-word units like "nautical mile"
            // Only combine here if that yields well-formed "square meters" or "cubic feet"
            if (IsLikelyMultiWordUnit(two))
            {
                unitToken = two;
                idx++;
            }
        }

        if (!TryExtractUnitAndExponent(unitToken, out fromUnit, out var expDim))
            return false;

        if (expDim != UnitDimension.Linear)
        {
            dimension = expDim;
        }

        return !string.IsNullOrWhiteSpace(fromUnit);
    }

    private static bool TryParseRightSide(string right, out string toUnit, out UnitDimension dimension)
    {
        toUnit = string.Empty;
        dimension = UnitDimension.Linear;

        var tokens = Tokenize(right);
        if (tokens.Count == 0)
            return false;

        var idx = 0;

        if (TryParseDimensionPrefix(tokens[idx], out var dimPrefix))
        {
            dimension = dimPrefix;
            idx++;
        }

        if (idx >= tokens.Count)
            return false;

        var unitToken = tokens[idx];

        if (idx + 1 < tokens.Count)
        {
            var two = $"{tokens[idx]} {tokens[idx + 1]}";
            if (IsLikelyMultiWordUnit(two))
            {
                unitToken = two;
                idx++;
            }
        }

        if (!TryExtractUnitAndExponent(unitToken, out toUnit, out var expDim))
            return false;

        if (expDim != UnitDimension.Linear)
        {
            dimension = expDim;
        }

        // Reject connector being parsed as unit on the right
        if (IsConnector(toUnit))
            return false;

        return !string.IsNullOrWhiteSpace(toUnit);
    }

    private static bool TryParseNumber(List<string> tokens, CultureInfo culture, out double value, out int consumedTokens)
    {
        value = 0;
        consumedTokens = 0;

        // Numbers can span multiple tokens due to thousands separators (e.g. "1 000 000")
        // Try concatenating up to 3 tokens
        var maxCombine = Math.Min(3, tokens.Count);
        for (var take = 1; take <= maxCombine; take++)
        {
            var candidate = string.Concat(tokens.Take(take));

            // Remove spaces/nbsp/underscores inside number to handle "1 000" and "1_000"
            candidate = candidate.Replace(" ", string.Empty)
                                 .Replace("\u00A0", string.Empty)
                                 .Replace("_", string.Empty);

            if (double.TryParse(candidate, NumberStyles.Float | NumberStyles.AllowThousands, culture, out value))
            {
                consumedTokens = take;
                return true;
            }
        }

        return false;
    }

    private static bool TryParseDimensionPrefix(string token, out UnitDimension dimension)
    {
        dimension = UnitDimension.Linear;
        var t = token.ToLowerInvariant();
        if (t is "square" or "sq" or "sq.")
        {
            dimension = UnitDimension.Squared;
            return true;
        }
        if (t is "cubic" or "cu" or "cu.")
        {
            dimension = UnitDimension.Cubic;
            return true;
        }
        return false;
    }

    private static bool TryExtractUnitAndExponent(string token, out string unit, out UnitDimension dimension)
    {
        unit = token;
        dimension = UnitDimension.Linear;

        // Handle suffix exponents
        if (token.EndsWith(Sup2) || token.EndsWith("2"))
        {
            unit = token[..^1];
            dimension = UnitDimension.Squared;
        }
        else if (token.EndsWith(Sup3) || token.EndsWith("3"))
        {
            unit = token[..^1];
            dimension = UnitDimension.Cubic;
        }

        unit = unit.Trim();
        if (unit.Length == 0)
            return false;

        return true;
    }

    private static bool IsLikelyMultiWordUnit(string twoTokens)
    {
        // Keep flexible; allow modules to normalize
        // Common combos we want to allow: "square meters", "cubic feet", "nautical mile"
        var tl = twoTokens.ToLowerInvariant();
        return tl.StartsWith("square ") || tl.StartsWith("cubic ") || tl is "nautical mile";
    }

    private static bool IsConnector(string token)
        => Connectors.Any(c => token.Equals(c, StringComparison.OrdinalIgnoreCase));

    private static List<string> Tokenize(string input)
    {
        input = NormalizeWhitespace(input);
        return input.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private static string NormalizeWhitespace(string s)
    {
        s = s.Replace('\u00A0', ' '); // NBSP -> space
        var sb = new StringBuilder(s.Length);
        var prevSpace = false;
        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }
        return sb.ToString().Trim();
    }
}