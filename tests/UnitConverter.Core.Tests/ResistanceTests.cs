using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class ResistanceTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("4.7 kohm to ohm", 4700)]
    [InlineData("4.7 kΩ to Ω", 4700)]
    [InlineData("1 mΩ to Ω", 0.001)]
    [InlineData("1 MΩ to Ω", 1000000)]
    [InlineData("1 mohm to ohm", 0.001)]
    [InlineData("1 Mohm to ohm", 1000000)]
    [InlineData("1 Mohm to mohm", 1000000000)]
    [InlineData("1 µΩ to Ω", 0.000001)]
    [InlineData("1 uohm to mohm", 0.001)]
    [InlineData("1 μΩ to mΩ", 0.001)]
    [InlineData("2 MILLIOHMS to ohms", 0.002)]
    [InlineData("2 megohms to kiloohms", 2000)]
    [InlineData("0 Ω to mΩ", 0)]
    [InlineData("-3 Ω to mΩ", -3000)]
    [InlineData("1 kohm 500 ohm to kohm", 1.5)]
    [InlineData("(2 + 3) / 2 kohm to ohm", 2500)]
    public void ConvertsResistanceSymbolsNamesAndAsciiPrefixes(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(UnitType.Resistance, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 MOHM to ohm")]
    [InlineData("1 Ω to V")]
    [InlineData("1 Ω to A")]
    [InlineData("1 Ω² to mΩ²")]
    [InlineData("1e308 Mohm to ohm")]
    public void RejectsAmbiguousCasingOtherQuantitiesDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseResistanceUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 mégohms en kilohms", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1500, result.Output.Value, 9);
        Assert.Equal("kilohm", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 Mohm", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "ohm" && item.Output.Value == 1000000);
        var incomplete = _service.ConvertQuery("1 ohm en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Resistance, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}