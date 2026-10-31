using System.Text.RegularExpressions;

namespace JPSoftworks.UnitConverter.Core;

internal sealed class NumericExpression(string text, CultureInfo culture)
{
    private int _position;
    private int _depth;

    public static bool TryRead(string text, CultureInfo culture, out double value, out int consumed)
    {
        value = 0;
        consumed = 0;
        if (text.Length > 4096)
            return false;

        try
        {
            var parser = new NumericExpression(text, culture);
            value = parser.ReadSum();
            consumed = parser._position;
            return double.IsFinite(value);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private double ReadSum()
    {
        var value = ReadProduct();
        while (true)
        {
            if (Take('+'))
            {
                value += ReadProduct();
            }
            else if (Take('-') || Take('−'))
            {
                value -= ReadProduct();
            }
            else
            {
                return value;
            }
        }
    }

    private double ReadProduct()
    {
        var value = ReadUnary();
        while (true)
        {
            if (Take('*') || Take('×'))
            {
                value *= ReadUnary();
            }
            else if (Take('/') || Take('÷'))
            {
                value /= ReadUnary();
            }
            else
            {
                return value;
            }
        }
    }

    private double ReadUnary()
    {
        if (++_depth > 32)
            throw new FormatException();

        double value;
        if (Take('+'))
        {
            value = ReadUnary();
        }
        else if (Take('-') || Take('−'))
        {
            value = -ReadUnary();
        }
        else
        {
            if (Take('('))
            {
                value = ReadSum();
                if (!Take(')')) throw new FormatException();
            }
            else
            {
                value = ReadNumber();
            }

            if (Take('^'))
            {
                value = Math.Pow(value, ReadUnary());
            }
        }

        _depth--;
        return value;
    }

    private double ReadNumber()
    {
        SkipWhitespace();
        if (_position < text.Length && Fraction(text[_position]) is double fraction)
        {
            _position++;
            return fraction;
        }

        var match = Regex.Match(text[_position..], @"^(?:\d+(?:[ _'\u2019\u00A0\u202F]\d{3}(?!\d))*(?:[.,]\d+)*|[.,]\d+)(?:[eE][+\-]?\d+)?");
        if (!match.Success)
            throw new FormatException();

        var number = match.Value.Replace(" ", "").Replace("_", "").Replace("\u00A0", "").Replace("\u202F", "");
        var groupSeparator = culture.NumberFormat.NumberGroupSeparator;
        if (groupSeparator is "'" or "\u2019")
            number = number.Replace("'", groupSeparator).Replace("\u2019", groupSeparator);
        var value = 0.0;
        // A zero integer part or an exponent makes a single punctuation mark decimal, not grouping.
        if (number.Contains('e') || number.Contains('E') || Regex.IsMatch(number, @"^0+[.,]\d+$"))
        {
            if (!double.TryParse(number.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                throw new FormatException();
        }
        else if (!(HasValidGrouping(number, culture.NumberFormat) &&
                   double.TryParse(number, NumberStyles.Float | NumberStyles.AllowThousands, culture, out value)) &&
                 !(HasValidGrouping(number, CultureInfo.InvariantCulture.NumberFormat) &&
                   double.TryParse(number, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value)))
            throw new FormatException();

        _position += match.Length;
        var numberEnd = _position;
        SkipWhitespace();
        if (value == Math.Truncate(value))
        {
            if (_position < text.Length && Fraction(text[_position]) is double suffix)
            {
                _position++;
                return value + suffix;
            }

            if (_position > numberEnd)
            {
                var mixed = Regex.Match(text[_position..], @"^(?<numerator>\d+)\s*/\s*(?<denominator>\d+)(?![\d.,])");
                if (mixed.Success)
                {
                    var numerator = double.Parse(mixed.Groups["numerator"].Value, CultureInfo.InvariantCulture);
                    var denominator = double.Parse(mixed.Groups["denominator"].Value, CultureInfo.InvariantCulture);
                    if (denominator == 0 || numerator >= denominator) throw new FormatException();
                    _position += mixed.Length;
                    return value + numerator / denominator;
                }
            }
        }

        return value;
    }

    private static bool HasValidGrouping(string number, NumberFormatInfo format)
    {
        var separator = format.NumberGroupSeparator;
        if (separator.Length == 0 || separator == format.NumberDecimalSeparator || !number.Contains(separator, StringComparison.Ordinal))
            return true;

        var parts = number.Split(['e', 'E'], 2)[0].Split(format.NumberDecimalSeparator, StringSplitOptions.None);
        if (parts.Skip(1).Any(part => part.Contains(separator, StringComparison.Ordinal)))
            return false;
        var groups = parts[0].Split(separator, StringSplitOptions.None);
        if (groups.Length == 1)
            return true;
        var sizes = format.NumberGroupSizes;
        if (sizes.Length == 0)
            return false;
        var index = 0;
        for (var i = groups.Length - 1; i > 0; i--, index++)
        {
            var size = sizes[Math.Min(index, sizes.Length - 1)];
            if (size == 0 || groups[i].Length != size)
                return false;
        }
        var firstSize = sizes[Math.Min(index, sizes.Length - 1)];
        return groups[0].Length > 0 && groups[0][0] != '0' && (firstSize == 0 || groups[0].Length <= firstSize);
    }

    private bool Take(char token)
    {
        SkipWhitespace();
        if (_position >= text.Length || text[_position] != token)
            return false;
        _position++;
        return true;
    }

    private void SkipWhitespace()
    {
        while (_position < text.Length && char.IsWhiteSpace(text[_position]))
        {
            _position++;
        }
    }

    private static double? Fraction(char token) => token switch
    {
        '½' => 0.5, '¼' => 0.25, '¾' => 0.75,
        '⅓' => 1.0 / 3, '⅔' => 2.0 / 3,
        '⅛' => 0.125, '⅜' => 0.375, '⅝' => 0.625, '⅞' => 0.875,
        '⅕' => 0.2, '⅖' => 0.4, '⅗' => 0.6, '⅘' => 0.8,
        '⅙' => 1.0 / 6, '⅚' => 5.0 / 6,
        _ => null
    };
}