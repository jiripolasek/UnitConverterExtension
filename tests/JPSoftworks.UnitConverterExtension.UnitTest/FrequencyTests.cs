using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class FrequencyTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1000 Hz to kHz", 1)]
    [InlineData("1 MHz to Hz", 1000000)]
    [InlineData("1 mHz to Hz", 0.001)]
    [InlineData("1 GHz to MHz", 1000)]
    [InlineData("1 THz to GHz", 1000)]
    [InlineData("1 µHz to mHz", 0.001)]
    [InlineData("1 uHz to Hz", 0.000001)]
    [InlineData("60 rpm to Hz", 1)]
    [InlineData("120 beats per minute to Hz", 2)]
    [InlineData("1 Hz to rad/s", 6.283185307179586)]
    [InlineData("1 rps to rpm", 60)]
    [InlineData("1 1/s to Hz", 1)]
    [InlineData("1 s^-1 to Hz", 1)]
    [InlineData("1 s⁻¹ to Hz", 1)]
    [InlineData("0 Hz to rpm", 0)]
    [InlineData("-1 kHz to Hz", -1000)]
    [InlineData("1 kHz 500 Hz to Hz", 1500)]
    [InlineData("2^10 Hz to kHz", 1.024)]
    public void ConvertsPrefixesCyclesAndAngularFrequency(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
        Assert.Equal(UnitType.Frequency, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 mhz to Hz")]
    [InlineData("1 Hz to s")]
    [InlineData("1 Hz to rad")]
    [InlineData("1 Hz² to kHz²")]
    [InlineData("1e308 THz to Hz")]
    public void RejectsAmbiguousCasingDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseFrequencyUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 mégahertz en kilohertz", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1500, result.Output.Value, 8);
        Assert.Equal("kilohertz", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("60 tours par minute", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "Hz" && item.Output.Value == 1);
        var incomplete = _service.ConvertQuery("60 rpm en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Frequency, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}