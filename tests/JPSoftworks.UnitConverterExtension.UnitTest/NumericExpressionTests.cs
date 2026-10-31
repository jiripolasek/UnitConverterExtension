using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class NumericExpressionTests
{
    [Theory]
    [InlineData("(1 + 2) m to cm", 300)]
    [InlineData("1 + 2 * 3 m to cm", 700)]
    [InlineData("2 * (3 + 4) m to cm", 1400)]
    [InlineData("-(1 + 2) m to cm", -300)]
    [InlineData("1/2 in to mm", 12.7)]
    [InlineData("½ in to mm", 12.7)]
    [InlineData("1½ in to mm", 38.1)]
    [InlineData("1 1/2 in to mm", 38.1)]
    [InlineData("-1 1/2 in to mm", -38.1)]
    [InlineData("1e3 m to km", 1)]
    [InlineData("1e-3 m to mm", 1)]
    [InlineData("1_000 m to km", 1)]
    [InlineData("1 000 m to km", 1)]
    [InlineData("1 m => cm", 100)]
    [InlineData("2 × 3 m to cm", 600)]
    [InlineData("1 ft ½ in to cm", 31.75)]
    [InlineData("2^3 m to cm", 800)]
    [InlineData("2 ^ 3 m to cm", 800)]
    [InlineData("(2^3) m to cm", 800)]
    [InlineData("2^3^2 m to m", 512)]
    [InlineData("-2^2 m to m", -4)]
    [InlineData("(-2)^2 m to m", 4)]
    [InlineData("2^-2 m to m", 0.25)]
    [InlineData("2^3 m2 to cm2", 80000)]
    public void NumericSyntaxConvertsCorrectly(string query, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("1 / 0 m to cm")]
    [InlineData("0 / 0 m to cm")]
    [InlineData("1e309 m to cm")]
    [InlineData("1e308 m to cm")]
    [InlineData("1e308 km3 to l")]
    [InlineData("(1 + 2 m to cm")]
    [InlineData("1 + * 2 m to cm")]
    [InlineData("2 ** 3 m to cm")]
    [InlineData("2^ m to cm")]
    [InlineData("10^1000 m to cm")]
    [InlineData("(-1)^0.5 m to cm")]
    [InlineData("0^-1 m to cm")]
    public void InvalidArithmeticIsRejected(string query)
    {
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void LocalDecimalArithmeticWorks()
    {
        var result = new ConversionService().ConvertQuery("(1,5 + 2,5) m en cm", CultureInfo.GetCultureInfo("fr-FR"));
        Assert.True(result.Success);
        Assert.Equal(400, result.Output.Value);
    }

    [Fact]
    public void ExcessiveNestingIsRejected()
    {
        var query = new string('(', 40) + "1" + new string(')', 40) + " m to cm";
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture).Success);
        Assert.False(new ConversionService().ConvertQuery(string.Concat(Enumerable.Repeat("2^", 40)) + "2 m to cm",
            CultureInfo.InvariantCulture).Success);
    }

    [Theory]
    [InlineData("de-DE", "1.5 m -> cm", 150)]
    [InlineData("de-DE", "1.234 m -> cm", 123400)]
    [InlineData("de-DE", "1.234,5 m -> cm", 123450)]
    [InlineData("en-US", "1,234.5 m -> cm", 123450)]
    [InlineData("cs-CZ", "1,234 m -> cm", 123.4)]
    [InlineData("cs-CZ", "1.234 m -> cm", 123.4)]
    [InlineData("hi-IN", "1,23,456 m -> m", 123456)]
    [InlineData("de-CH", "1’000 m -> m", 1000)]
    [InlineData("de-CH", "1'000 m -> m", 1000)]
    [InlineData("de-CH", "1’234.5 m -> m", 1234.5)]
    [InlineData("en-IN", "100,000 m -> m", 100000)]
    [InlineData("de-DE", "1,234.5 m -> m", 1234.5)]
    public void RegionalGroupingAndDecimalFallbackHaveStableMeanings(string regionalName, string query, double value)
    {
        foreach (var uiName in new[] { "en-US", "fr-FR", "ja-JP" })
        {
            var result = new ConversionService(null, null, CultureInfo.GetCultureInfo(uiName))
                .ConvertQuery(query, CultureInfo.GetCultureInfo(regionalName));
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(value, result.Output.Value, 8);
        }
    }

    [Theory]
    [InlineData("1,5 m -> cm")]
    [InlineData("12,34 m -> cm")]
    [InlineData("1,2345 m -> cm")]
    public void MalformedGroupingDoesNotSilentlyMultiplyTheValue(string query)
    {
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.GetCultureInfo("en-US")).Success);
    }

    [Theory]
    [InlineData("de-DE", "0.500 m", 0.5)]
    [InlineData("de-DE", "0.125 km to m", 125)]
    [InlineData("de-DE", "1.500e3 m", 1500)]
    [InlineData("it-IT", "0.750 l to ml", 750)]
    [InlineData("pt-BR", "0.250 kg to g", 250)]
    [InlineData("es-ES", "0.500 kg to g", 500)]
    [InlineData("nl-NL", "0.750 m to cm", 75)]
    [InlineData("en-US", "0,500 m", 0.5)]
    [InlineData("en-US", "1,500e3 m", 1500)]
    [InlineData("de-DE", "-0.500 m", -0.5)]
    [InlineData("en-US", "00,500 m", 0.5)]
    [InlineData("de-DE", "0.500e3 m", 500)]
    public void FractionsAndScientificNotationNeverUsePunctuationAsGrouping(string cultureName, string query, double value)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.GetCultureInfo(cultureName));
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(value, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("de-DE", "0.500.000 m")]
    [InlineData("en-US", "0,500,000 m")]
    [InlineData("de-DE", "1.500.000e3 m")]
    [InlineData("en-US", "1,500,000e3 m")]
    public void InvalidZeroOrScientificGroupingIsRejected(string cultureName, string query) =>
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.GetCultureInfo(cultureName)).Success);
}