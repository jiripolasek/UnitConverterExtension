using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core;

public partial class ConversionService
{
    private ConversionResult? TryConvertCompound(ParsedConversionQuery query, IConversionModule module, string source, CultureInfo culture)
    {
        if (module is not (UnitsNetLengthModule or UnitsNetTimeModule or UnitsNetMassModule))
            return null;

        var targets = query.ToUnit.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (targets.Length is < 2 or > 4)
            return null;

        var tokens = new UnitToken[targets.Length];
        for (var i = 0; i < targets.Length; i++)
        {
            if (!module.TryNormalizeUnit(targets[i], culture, out var unit))
                return null;
            targets[i] = unit;
            tokens[i] = new UnitToken { Unit = unit, Type = ResolveUnitType(module, UnitDimension.Linear), Metadata = module.GetUnitMetadata(unit, this._uiCulture ?? culture) };
        }

        var factors = new double[targets.Length];
        factors[^1] = 1;
        for (var i = targets.Length - 2; i >= 0; i--)
        {
            if (!module.TryConvert(1, targets[i], targets[i + 1], out var ratio) || !double.IsFinite(ratio))
                return null;
            var radix = Math.Round(ratio);
            // Compound fields require descending units with an integral carry, such as 12 inches per foot.
            if (radix < 2 || Math.Abs(ratio - radix) > radix * 1e-12)
                return null;
            factors[i] = radix * factors[i + 1];
            if (!double.IsFinite(factors[i]))
                return null;
        }

        if (!module.TryConvert(query.Value, source, targets[^1], out var total) || !double.IsFinite(total) ||
            !module.TryConvert(query.Value, source, targets[0], out var scalar) || !double.IsFinite(scalar))
            return null;

        var remainder = Math.Abs(total);
        var tolerance = 4 * (remainder - Math.BitDecrement(remainder));
        var sign = Math.Sign(total);
        var components = new Quantity[targets.Length];
        var displayValues = new double[targets.Length];
        for (var i = 0; i < targets.Length; i++)
        {
            var value = i == targets.Length - 1 ? remainder : Math.Floor(remainder / factors[i]);
            remainder = Math.Max(0, remainder - value * factors[i]);
            // Discard subtraction residue within a few ulps of the total.
            if (value > 0 && remainder <= tolerance)
                remainder = 0;
            components[i] = new Quantity(value == 0 ? 0 : sign * value, tokens[i]);
            displayValues[i] = value;
        }

        displayValues[^1] = double.Parse(FormatNumber(displayValues[^1], culture), NumberStyles.Float, culture);
        for (var i = targets.Length - 1; i > 0; i--)
        {
            var radix = factors[i - 1] / factors[i];
            if (displayValues[i] >= radix)
            {
                displayValues[i] -= radix;
                displayValues[i - 1]++;
            }
        }

        return new ConversionResult
        {
            Success = true,
            Output = new Quantity(scalar, tokens[0]),
            OutputComponents = components,
            FormattedResult = string.Join(" ", displayValues.Select((value, i) =>
                FormatResult(value == 0 ? 0 : sign * value, targets[i], UnitDimension.Linear, culture)))
        };
    }
}
