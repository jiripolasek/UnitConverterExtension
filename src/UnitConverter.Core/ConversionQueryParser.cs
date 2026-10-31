// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Text.RegularExpressions;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core;

public static class ConversionQueryParser
{
    private static readonly Dictionary<string, UnitDimension> DimensionKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Squared indicators
        ["square"] = UnitDimension.Squared,
        ["carré"] = UnitDimension.Squared,
        ["carrés"] = UnitDimension.Squared,
        ["carre"] = UnitDimension.Squared,
        ["carres"] = UnitDimension.Squared,
        ["sq"] = UnitDimension.Squared,
        ["²"] = UnitDimension.Squared,
        ["^2"] = UnitDimension.Squared,

        // Cubic indicators
        ["cubic"] = UnitDimension.Cubic,
        ["cube"] = UnitDimension.Cubic,
        ["cubes"] = UnitDimension.Cubic,
        ["cu"] = UnitDimension.Cubic,
        ["³"] = UnitDimension.Cubic,
        ["^3"] = UnitDimension.Cubic
    };

    private static readonly string[] SymbolConversionIndicators = ["->", "=>", "=", "→"];
    private static readonly string SymbolConversionIndicatorPattern = string.Join('|', SymbolConversionIndicators.Select(Regex.Escape));

    public static ParsedConversionQuery? TryParseQuery(string query, CultureInfo culture) => TryParseQuery(query, culture, null);

    internal static ParsedConversionQuery? TryParseQuery(string query, CultureInfo culture, IEnumerable<IConversionModule>? modules,
        ConversionWordLanguage wordLanguage = ConversionWordLanguage.Default)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length > 4096)
        {
            return null;
        }

        query = query.Trim();

        // Try explicit conversion first (e.g., "1 m to cm", "1m->cm", "10°C as F")
        var explicitResult = TryParseExplicitConversion(query, culture, modules, wordLanguage);
        if (explicitResult != null)
        {
            return explicitResult;
        }

        // Try implicit conversion (e.g., "60 mins", "100 cm", "5 cubic feet")
        var implicitResult = TryParseImplicitConversion(query, culture, modules, wordLanguage);
        if (implicitResult != null)
        {
            return implicitResult;
        }

        return null;
    }

    internal static (Match Indicator, int TargetEnd) FindConversionBoundary(string query, CultureInfo culture, IEnumerable<IConversionModule>? modules,
        ConversionWordLanguage wordLanguage = ConversionWordLanguage.Default)
    {
        var symbol = Regex.Match(query, SymbolConversionIndicatorPattern);
        if (symbol.Success)
            return (symbol, query.Length);

        var wordPattern = @"(?<!\S)(?:" +
            string.Join('|', ConverterText.GetConversionWords(culture, wordLanguage).Select(Regex.Escape)) + @")(?!\S)";
        var nativePattern = string.Join('|', ConverterText.GetQueryLanguages(culture)
            .Select(ConverterText.GetCjkQueryLanguage).Distinct().Select(language => language switch
        {
            "ja" => @"(?:を|から)(?=\s*(?<target>.*?)(?:\s*(?:に|へ)\s*(?:(?:変換|換算)(?:する)?)?)?\s*$)",
            "ko" => @"(?:에서|을|를)(?=\s*(?<target>.*?)(?:\s*(?:으로|로)\s*(?:(?:변환|환산)(?:하기|하다)?)?)?\s*$)",
            "zh" => "(?:" + string.Join('|', ConverterText.GetConversionWords(culture, wordLanguage)
                .Where(word => word.Any(character => character is >= '\u3400' and <= '\u9fff'))
                .OrderByDescending(word => word.Length).Select(Regex.Escape)) + @")(?=\s*(?<target>.*?)\s*$)",
            _ => string.Empty
        }).Where(pattern => pattern.Length > 0));
        var pattern = nativePattern.Length > 0 ? $"(?:{nativePattern})|(?:{wordPattern})" : wordPattern;
        var indicators = Regex.Matches(query, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (indicators.Count == 0 || HasKnownUnits(ParseInput(query, culture, modules, wordLanguage)))
            return (Match.Empty, query.Length);

        var recognized = Match.Empty;
        var fallback = Match.Empty;
        foreach (Match indicator in indicators)
        {
            var components = ParseInput(query[..indicator.Index].Trim(), culture, modules, wordLanguage);
            if (components.Length == 0)
            {
                continue;
            }

            if (!fallback.Success)
            {
                fallback = indicator;
            }
            // Prefer the longest recognized source so words inside unit names retain their unit meaning.
            if (HasKnownUnits(components))
            {
                recognized = indicator;
            }
        }

        var selected = recognized.Success ? recognized : fallback;
        var target = selected.Groups["target"];
        var targetEnd = target.Success ? target.Index + target.Length : query.Length;
        if (target.Success)
        {
            // Preserve a complete unit spelling before treating its ending as grammar.
            var (unit, _) = ParseUnitWithDimension(query[(selected.Index + selected.Length)..].Trim());
            if (IsKnownUnit(unit, culture, modules))
            {
                targetEnd = query.Length;
            }
        }
        return (selected, targetEnd);

        bool HasKnownUnits(Quantity[] components) => components.Length > 0 && components.All(component => modules == null
            ? string.Equals(component.Unit.Unit, "in", StringComparison.OrdinalIgnoreCase) ||
              !Regex.IsMatch(component.Unit.Unit, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
            : IsKnownUnit(component.Unit.Unit, culture, modules));
    }

    private static ParsedConversionQuery? TryParseExplicitConversion(string query, CultureInfo culture, IEnumerable<IConversionModule>? modules,
        ConversionWordLanguage wordLanguage)
    {
        // Share separator and target-suffix boundaries with guidance and casing choices.
        var (match, targetEnd) = FindConversionBoundary(query, culture, modules, wordLanguage);

        if (!match.Success)
        {
            return null;
        }

        var indicator = match.Value.Trim();
        var indicatorIndex = match.Index;

        // Split the query at the conversion indicator
        var leftPart = query[..indicatorIndex].Trim();
        var rightPart = query[(indicatorIndex + indicator.Length)..targetEnd].Trim();

        if (string.IsNullOrWhiteSpace(leftPart) || string.IsNullOrWhiteSpace(rightPart))
        {
            return null;
        }

        var components = ParseInput(leftPart, culture, modules, wordLanguage);
        if (components.Length == 0 || IsConnector(rightPart, culture, modules, wordLanguage))
        {
            return null;
        }

        var fromUnit = components[0].Unit.Unit;
        var fromDimension = components[0].Unit.Dimension;
        var (toUnit, toDimension) = ParseUnitWithDimension(rightPart);

        if (fromDimension != UnitDimension.Linear && toDimension != UnitDimension.Linear && fromDimension != toDimension)
        {
            return null;
        }

        // Use the dimension from either unit (only one needs to be specified for squared/cubic)
        var dimension = fromDimension != UnitDimension.Linear ? fromDimension : toDimension;

        return new ParsedConversionQuery
        {
            Value = components[0].Value,
            FromUnit = fromUnit,
            ToUnit = toUnit,
            Dimension = dimension,
            FromDimension = fromDimension,
            ToDimension = toDimension,
            Components = components.Length > 1 ? components : [],
            IsImplicitConversion = false,
            OriginalQuery = query
        };
    }

    private static ParsedConversionQuery? TryParseImplicitConversion(string query, CultureInfo culture, IEnumerable<IConversionModule>? modules,
        ConversionWordLanguage wordLanguage)
    {
        var components = ParseInput(query, culture, modules, wordLanguage);
        if (components.Length == 0)
        {
            return null;
        }

        return new ParsedConversionQuery
        {
            Value = components[0].Value,
            FromUnit = components[0].Unit.Unit,
            ToUnit = string.Empty,
            Dimension = components[0].Unit.Dimension,
            FromDimension = components[0].Unit.Dimension,
            Components = components.Length > 1 ? components : [],
            IsImplicitConversion = true,
            OriginalQuery = query
        };
    }

    private static Quantity[] ParseInput(string text, CultureInfo culture, IEnumerable<IConversionModule>? modules, ConversionWordLanguage wordLanguage)
    {
        var compact = Regex.Match(text, @"^(?<sign>[+\-−]?)\s*(?<feet>\d+(?:[.,]\d+)?)\s*(?:['′’]|ft(?=[\d.,]))\s*(?<inches>.+?)\s*(?:[""″”]|in(?:ch(?:es)?)?)$", RegexOptions.IgnoreCase);
        if (compact.Success)
        {
            var inches = compact.Groups["inches"].Value.Trim();
            if (!NumericExpression.TryRead(inches, culture, out var value, out var consumed) || consumed != inches.Length || value < 0)
                return [];
            var sign = compact.Groups["sign"].Value.Replace('−', '-');
            text = $"{sign}{compact.Groups["feet"].Value} ft {sign}({inches}) in";
        }

        var parts = Regex.Split(text, @"(?<=[^\s+\-])\s+(?=[+\-]?\s*[\d(½¼¾⅓⅔⅛⅜⅝⅞⅕⅖⅗⅘⅙⅚])");
        if (parts.Length > 32)
            return [];

        if (parts.Length > 1)
        {
            var components = new List<Quantity>();
            var pending = string.Empty;
            foreach (var part in parts)
            {
                pending = pending.Length == 0 ? part : $"{pending} {part}";
                if (modules == null && Regex.IsMatch(pending, @"\b(?:per|par|aux|pour)\s*$", RegexOptions.IgnoreCase))
                {
                    continue;
                }

                var component = ParseQuantity(pending, culture, modules, wordLanguage);
                if (component == null || modules != null && !IsKnownUnit(component.Unit.Unit, culture, modules))
                {
                    continue;
                }

                components.Add(component);
                pending = string.Empty;
            }

            if (pending.Length == 0)
                return components.ToArray();
        }

        var single = ParseQuantity(text, culture, modules, wordLanguage);
        return single == null ? [] : [single];
    }

    private static Quantity? ParseQuantity(string text, CultureInfo culture, IEnumerable<IConversionModule>? modules, ConversionWordLanguage wordLanguage)
    {
        // shortcut: clock input uses min/km or min/mi; expand when other clock formats are supported.
        var pace = Regex.Match(text, @"^(?<minutes>\d+):(?<seconds>[0-5]\d(?:[.,]\d+)?)\s*(?<unit>.+)$", RegexOptions.IgnoreCase);
        if (pace.Success)
        {
            var speed = modules?.OfType<UnitsNetSpeedModule>().FirstOrDefault() ?? new UnitsNetSpeedModule();
            if (!speed.TryNormalizeUnit(pace.Groups["unit"].Value.Trim(), culture, out var paceUnit) || paceUnit is not ("min/km" or "min/mi") ||
                !double.TryParse(pace.Groups["minutes"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
                (!double.TryParse(pace.Groups["seconds"].Value, NumberStyles.AllowDecimalPoint, culture, out var seconds) &&
                 !double.TryParse(pace.Groups["seconds"].Value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out seconds)) ||
                !double.IsFinite(minutes))
                return null;
            return new Quantity(minutes + seconds / 60, new UnitToken { Unit = paceUnit });
        }

        if (!NumericExpression.TryRead(text, culture, out var value, out var consumed))
            return null;

        var (unit, dimension) = ParseUnitWithDimension(text[consumed..].Trim());
        return string.IsNullOrWhiteSpace(unit) || IsConnector(unit, culture, modules, wordLanguage)
            ? null
            : new Quantity(value, new UnitToken { Unit = unit, Dimension = dimension });
    }

    private static (string unit, UnitDimension dimension) ParseUnitWithDimension(string unitStr)
    {
        // Powers within compound units such as m/s² belong to the unit, not the whole quantity.
        if (unitStr.Contains('/') || unitStr.Contains('°') || unitStr.Contains(" par ", StringComparison.OrdinalIgnoreCase))
        {
            return (unitStr, UnitDimension.Linear);
        }

        // Check for dimension indicators
        var dimension = UnitDimension.Linear;
        var unit = unitStr;

        // Check for superscript numbers or symbols
        if (unitStr.EndsWith('²') || unitStr.EndsWith("^2", StringComparison.Ordinal))
        {
            dimension = UnitDimension.Squared;
            unit = unitStr.Replace("²", "").Replace("^2", "");
        }
        else if (unitStr.EndsWith('³') || unitStr.EndsWith("^3", StringComparison.Ordinal))
        {
            dimension = UnitDimension.Cubic;
            unit = unitStr.Replace("³", "").Replace("^3", "");
        }
        else if (Regex.IsMatch(unitStr, @"\S\d$"))
        {
            // Digits must be attached to the unit; a trailing value still needs its own unit.
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
                else if (DimensionKeywords.TryGetValue(words[^1], out dim))
                {
                    dimension = dim;
                    unit = string.Join(" ", words[..^1]);
                }
            }
        }

        return (unit.Trim(), dimension);
    }

    private static bool IsKnownUnit(string unit, CultureInfo culture, IEnumerable<IConversionModule>? modules) =>
        modules?.Any(module => module.TryNormalizeUnit(unit, culture, out _) ||
            module is DescriptorBasedConversionModule descriptor && descriptor.HasUnitSpelling(unit, culture)) == true;

    private static bool IsConnector(string text, CultureInfo culture, IEnumerable<IConversionModule>? modules, ConversionWordLanguage wordLanguage)
    {
        var t = text.Trim();
        return SymbolConversionIndicators.Contains(t, StringComparer.Ordinal) ||
               ConverterText.GetConversionWords(culture, wordLanguage).Contains(t, StringComparer.OrdinalIgnoreCase) &&
               !IsKnownUnit(t, culture, modules) &&
               !(modules == null && string.Equals(t, "in", StringComparison.OrdinalIgnoreCase));
    }
}
