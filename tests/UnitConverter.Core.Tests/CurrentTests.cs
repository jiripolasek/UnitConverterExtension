using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class CurrentTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("500 mA to A", 0.5)]
    [InlineData("1 MA to A", 1000000)]
    [InlineData("1 kA to A", 1000)]
    [InlineData("1 µA to A", 0.000001)]
    [InlineData("1 uA to mA", 0.001)]
    [InlineData("1 μA to mA", 0.001)]
    [InlineData("2 MILLIAMPS to A", 0.002)]
    [InlineData("2 amps to mA", 2000)]
    [InlineData("0 A to mA", 0)]
    [InlineData("-3 A to mA", -3000)]
    [InlineData("1 A 500 mA to A", 1.5)]
    [InlineData("2^3 A to mA", 8000)]
    public void ConvertsCurrentPrefixesAndExpressions(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(UnitType.Current, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 ma to A")]
    [InlineData("1 A to V")]
    [InlineData("1 A to W")]
    [InlineData("1 Ah to A")]
    [InlineData("1 A² to mA²")]
    [InlineData("1e308 MA to A")]
    public void RejectsAmbiguousCasingOtherQuantitiesDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseCurrentUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 mégaampères en kiloampères", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1500, result.Output.Value, 9);
        Assert.Equal("kiloampère", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 A", french);
        Assert.Equal(UnitType.Current, implicitResult.Output.Unit.Type);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "mA" && item.Output.Value == 1000);
        var incomplete = _service.ConvertQuery("1 A en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Current, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}