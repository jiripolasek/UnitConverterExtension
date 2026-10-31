using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class VoltageTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("3.3 V to mV", 3300)]
    [InlineData("1 mV to V", 0.001)]
    [InlineData("1 MV to V", 1000000)]
    [InlineData("1 kV to V", 1000)]
    [InlineData("1 µV to V", 0.000001)]
    [InlineData("1 uV to mV", 0.001)]
    [InlineData("1 μV to mV", 0.001)]
    [InlineData("2 MILLIVOLTS to V", 0.002)]
    [InlineData("0 V to mV", 0)]
    [InlineData("-3 V to mV", -3000)]
    [InlineData("1 V 500 mV to V", 1.5)]
    [InlineData("(2 + 3) / 2 V to mV", 2500)]
    public void ConvertsVoltagePrefixesAndExpressions(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(UnitType.Voltage, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 mv to V")]
    [InlineData("1 V to W")]
    [InlineData("1 V² to mV²")]
    [InlineData("1e308 MV to V")]
    public void RejectsAmbiguousCasingOtherQuantitiesDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseVoltageUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 mégavolts en kilovolts", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1500, result.Output.Value, 9);
        Assert.Equal("kilovolt", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 millivolt", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "V" && item.Output.Value == 0.001);
        var incomplete = _service.ConvertQuery("1 V en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Voltage, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}