using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class SuggestionValueTests
{
    [Theory]
    [InlineData("-40 °C", "°F", -40)]
    [InlineData("32 °F", "°C", 0)]
    [InlineData("0 °C", "°F", 32)]
    public void TemperatureSuggestionsIncludeZeroAndNegativeResults(string query, string unit, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        var suggestion = Assert.Single(result.Suggestions, suggestion => suggestion.Output.Unit.Unit == unit);
        Assert.Equal(expected, suggestion.Output.Value, 8);
    }

    [Theory]
    [InlineData("m")]
    [InlineData("kg")]
    [InlineData("h")]
    public void LinearSuggestionsRankByMagnitudeRegardlessOfSign(string unit)
    {
        var service = new ConversionService();
        var positive = service.ConvertQuery($"1 {unit}", CultureInfo.InvariantCulture).Suggestions;
        var negative = service.ConvertQuery($"-1 {unit}", CultureInfo.InvariantCulture).Suggestions;
        Assert.NotEmpty(positive);
        Assert.Equal(positive.Select(s => s.Output.Unit.Unit), negative.Select(s => s.Output.Unit.Unit));
        Assert.Equal(positive.Select(s => -s.Output.Value), negative.Select(s => s.Output.Value));
    }

    [Fact]
    public void ZeroAndExtremeFiniteValuesStillOfferConversions()
    {
        var service = new ConversionService();
        foreach (var query in new[] { "0 m", "0 kg", "1e-20 m", "1e20 m" })
        {
            var result = service.ConvertQuery(query, CultureInfo.InvariantCulture);
            Assert.NotEmpty(result.Suggestions);
            Assert.All(result.Suggestions, suggestion => Assert.True(double.IsFinite(suggestion.Output.Value)));
        }
        Assert.All(service.ConvertQuery("0 m").Suggestions, suggestion => Assert.Equal(0, suggestion.Output.Value));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonfiniteValuesNeverBecomeSuggestions(double value)
    {
        Assert.Empty(new UnitsNetLengthModule().GetCommonConversions(value, "m"));
    }
}