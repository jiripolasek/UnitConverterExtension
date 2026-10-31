using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class ReviewRegressionTests
{
    [Theory]
    [InlineData("1 mi to ft in", "5280 ft 0 in")]
    [InlineData("7 yd to ft in", "21 ft 0 in")]
    [InlineData("3 ft to yd ft in", "1 yd 0 ft 0 in")]
    [InlineData("2.54 cm to in mil", "1 in 0 mil")]
    [InlineData("-5 ft to ft in", "-5 ft 0 in")]
    [InlineData("-1 h to h min", "-1 h 0 min")]
    [InlineData("-1 lb to lb oz", "-1 lb 0 oz")]
    [InlineData("1200000000000.25 in to ft in", "100000000000 ft 0.25 in")]
    [InlineData("1e-10 s to h min s", "0 h 0 min 1E-10 s")]
    public void CompoundOutputDropsRoundoffAndNegativeZeroWithoutLosingSmallValues(string query, string expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.FormattedResult);
        Assert.All(result.OutputComponents.Where(component => component.Value == 0),
            component => Assert.Equal(0, BitConverter.DoubleToInt64Bits(component.Value)));
    }

    [Theory]
    [InlineData("es-MX", "es-ES", "1 metro a pies")]
    [InlineData("de-AT", "de-DE", "1 Meter zu Fuß")]
    [InlineData("de-CH", "de-DE", "1 Meter als Fuß")]
    [InlineData("it-CH", "it-IT", "1 metro in piedi")]
    [InlineData("nl-BE", "nl-NL", "1 meter naar voet")]
    [InlineData("pt-AO", "pt-PT", "1 metro em pés")]
    [InlineData("pt", "pt-PT", "1 metro em pés")]
    [InlineData("zh-Hans-CN", "zh-CN", "1米转英尺")]
    [InlineData("zh-Hant-TW", "zh-TW", "1公尺轉英尺")]
    [InlineData("zh-HK", "zh-TW", "1公尺轉英尺")]
    [InlineData("zh-MO", "zh-TW", "1公尺轉英尺")]
    [InlineData("cs", "cs-CZ", "1 metr na stopy")]
    [InlineData("ja", "ja-JP", "1メートルをフィートに")]
    public void RegionalAndScriptVariantsUseTheSupportedLanguage(string locale, string supported, string query)
    {
        var culture = CultureInfo.GetCultureInfo(locale);
        var language = CultureInfo.GetCultureInfo(supported);
        Assert.Equal(ConverterText.Get("Help", language), ConverterText.Get("Help", culture));
        Assert.Equal(ConverterText.GetUnitNames("Length", "m", language), ConverterText.GetUnitNames("Length", "m", culture));
        var result = new ConversionService().ConvertQuery(query, culture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1 / 0.3048, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("5:30 min/mile -> mph")]
    [InlineData("5:30 minutes per mile -> mph")]
    [InlineData("5:30 minute per mile -> mph")]
    public void ClockPaceUsesTheSpeedModulesAliases(string query)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(60 / 5.5, result.Output.Value, 8);
        var parsed = ConversionQueryParser.TryParseQuery("5:30 min/mile", CultureInfo.InvariantCulture);
        Assert.NotNull(parsed);
        Assert.Equal("min/mi", parsed.FromUnit);
        Assert.Equal(5.5, parsed.Value);
    }

    [Theory]
    [InlineData("10 mbps 5 Mbps")]
    [InlineData("10 MBPS 5 Mbps")]
    public void MixedDataRatesOfferTheSameCasingChoicesAsSingleValues(string query)
    {
        var service = new ConversionService();
        var culture = CultureInfo.InvariantCulture;
        var result = service.ConvertQuery(query + " -> Mb/s", culture);
        Assert.False(result.Success);
        Assert.True(result.IsIncomplete, result.ErrorMessage);
        Assert.InRange(result.OtherInterpretations.Length, 2, 3);
        var values = result.OtherInterpretations.Select(choice =>
        {
            var converted = service.ConvertQuery(choice.Query, culture);
            Assert.True(converted.Success, converted.ErrorMessage);
            return converted.Output.Value;
        }).Order().ToArray();
        Assert.Contains(15, values);
        Assert.Contains(85, values);
    }
}