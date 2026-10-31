using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class TemperatureDeltaTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("10 Δ°C to Δ°F", 18)]
    [InlineData("18 Δ°F to Δ°C", 10)]
    [InlineData("10 delta C to delta F", 18)]
    [InlineData("10 Celsius differences to Kelvin difference", 10)]
    [InlineData("18 Δ°R to ΔK", 10)]
    [InlineData("10 ∆K to ∆°C", 10)]
    [InlineData("-10 Δ°C to Δ°F", -18)]
    [InlineData("0 Δ°C to Δ°F", 0)]
    [InlineData("10 Δ°C 18 Δ°F to ΔK", 20)]
    [InlineData("(2 + 3) Δ°C to Δ°F", 9)]
    public void ConvertsDifferencesWithoutTemperatureOffsets(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
        Assert.Equal(UnitType.TemperatureDelta, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("10 °C to Δ°F")]
    [InlineData("10 Δ°C to °F")]
    [InlineData("10 ΔK to K")]
    [InlineData("10 Δ°C 18 °F to ΔK")]
    [InlineData("1 delta C² to delta F²")]
    [InlineData("1e308 Δ°C to Δ°F")]
    public void RejectsAbsoluteTemperaturesDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void AbsoluteTemperaturesStillUseOffsets()
    {
        var result = _service.ConvertQuery("10 °C to °F", CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(50, result.Output.Value, 8);
        Assert.Equal(UnitType.Temperature, result.Output.Unit.Type);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsKeepDifferencesSeparate()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("2,5 degrés Celsius d'écart en degrés Fahrenheit d'écart", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(4.5, result.Output.Value, 8);
        Assert.Equal("degré Fahrenheit d'écart", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("10 Δ°C", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "Δ°F" && item.Output.Value == 18);
        var incomplete = _service.ConvertQuery("10 delta C en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.TemperatureDelta, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}