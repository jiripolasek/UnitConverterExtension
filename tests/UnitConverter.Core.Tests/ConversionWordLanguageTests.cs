using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class ConversionWordLanguageTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("cs-CZ", ConversionWordLanguage.Default, "to", true)]
    [InlineData("cs-CZ", ConversionWordLanguage.Default, "in", true)]
    [InlineData("cs-CZ", ConversionWordLanguage.Default, "as", true)]
    [InlineData("cs-CZ", ConversionWordLanguage.Default, "na", true)]
    [InlineData("cs-CZ", ConversionWordLanguage.InputLanguageOnly, "to", false)]
    [InlineData("cs-CZ", ConversionWordLanguage.InputLanguageOnly, "na", true)]
    [InlineData("en-US", ConversionWordLanguage.InputLanguageOnly, "to", true)]
    [InlineData("en-US", ConversionWordLanguage.InputLanguageOnly, "as", true)]
    [InlineData("en-US", ConversionWordLanguage.InputLanguageOnly, "in", true)]
    [InlineData("en-US", ConversionWordLanguage.InputLanguageOnly, "en", false)]
    [InlineData("en-US", ConversionWordLanguage.InputLanguageAndEnglish, "vers", false)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageOnly, "en", true)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageOnly, "vers", true)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageOnly, "to", false)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageOnly, "as", false)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageOnly, "in", false)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageAndEnglish, "to", true)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageAndEnglish, "as", true)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageAndEnglish, "in", true)]
    [InlineData("fr-FR", ConversionWordLanguage.InputLanguageAndEnglish, "en", true)]
    [InlineData("fr-CA", ConversionWordLanguage.InputLanguageOnly, "vers", true)]
    [InlineData("fr-CA", ConversionWordLanguage.InputLanguageOnly, "in", false)]
    [InlineData("de-DE", ConversionWordLanguage.InputLanguageOnly, "in", true)]
    [InlineData("de-DE", ConversionWordLanguage.InputLanguageOnly, "to", false)]
    [InlineData("de-DE", ConversionWordLanguage.InputLanguageAndEnglish, "to", true)]
    [InlineData("hu-HU", ConversionWordLanguage.InputLanguageOnly, "to", true)]
    [InlineData("de-DE", ConversionWordLanguage.InputLanguageOnly, "en", false)]
    public void WordAvailabilityFollowsTheInputLanguageAndMode(string cultureName, ConversionWordLanguage mode, string word, bool expected)
    {
        var result = _service.ConvertQuery($"1 m {word} cm", CultureInfo.GetCultureInfo(cultureName), mode);
        Assert.Equal(expected, result.Success);
        if (expected)
        {
            Assert.Equal(100, result.Output.Value);
        }
    }

    [Theory]
    [InlineData("->")]
    [InlineData("=>")]
    [InlineData("=")]
    [InlineData("→")]
    public void SymbolsWorkInEveryMode(string symbol)
    {
        foreach (var mode in Enum.GetValues<ConversionWordLanguage>())
        {
            var result = _service.ConvertQuery($"1m{symbol}cm", CultureInfo.GetCultureInfo("fr-FR"), mode);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(100, result.Output.Value);
        }
    }

    [Theory]
    [InlineData("12 in en cm", "12 in->cm")]
    [InlineData("1 ft 2 in en cm", "1 ft 2 in->cm")]
    [InlineData("1 in Hg en Pa", "1 in Hg->Pa")]
    [InlineData("1 lbf in en Nm", "1 lbf in->Nm")]
    [InlineData("180 cm en ft in", "180 cm->ft in")]
    public void InactiveConnectorWordsStillWorkAsUnits(string query, string canonical)
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var expected = _service.ConvertQuery(canonical, culture, ConversionWordLanguage.InputLanguageOnly);
        var actual = _service.ConvertQuery(query, culture, ConversionWordLanguage.InputLanguageOnly);
        Assert.True(expected.Success, expected.ErrorMessage);
        Assert.True(actual.Success, actual.ErrorMessage);
        Assert.Equal(expected.Output.Value, actual.Output.Value, 8);
        Assert.Equal(expected.FormattedResult, actual.FormattedResult);
    }

    [Theory]
    [InlineData(ConversionWordLanguage.InputLanguageOnly, "en")]
    [InlineData(ConversionWordLanguage.InputLanguageAndEnglish, "in")]
    public void CompletionsAndInterpretationsKeepTheChosenMode(ConversionWordLanguage mode, string word)
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var incomplete = _service.ConvertQuery($"1 m {word} c", culture, mode);
        Assert.True(incomplete.IsIncomplete);
        Assert.Contains(incomplete.Completions, item => item.Query == $"1 m {word} cm");
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, culture, mode).Success));

        var interpreted = _service.ConvertQuery($"10 mN {word} N", culture, mode);
        Assert.True(interpreted.Success, interpreted.ErrorMessage);
        Assert.Contains(interpreted.OtherInterpretations, item => item.Query == $"10 MN {word} N");
        Assert.All(interpreted.OtherInterpretations, item => Assert.True(_service.ConvertQuery(item.Query, culture, mode).Success));
    }

    [Fact]
    public void DefaultResolvesSeparatelyFromExplicitModes()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var local = ConverterText.GetConversionWords(culture, ConversionWordLanguage.InputLanguageOnly);
        var bilingual = ConverterText.GetConversionWords(culture, ConversionWordLanguage.InputLanguageAndEnglish);
        Assert.Equal(["en", "vers"], local);
        Assert.Equal(["en", "vers", "to", "as", "in"], bilingual);
        var allLanguages = ConverterText.GetConversionWords(culture, ConversionWordLanguage.InterfaceInputAndEnglish);
        Assert.Equal(bilingual, allLanguages);
        Assert.Same(allLanguages, ConverterText.GetConversionWords(culture, ConversionWordLanguage.Default));
        Assert.Equal("Default", ConversionWordLanguage.Default.ToString());
        Assert.NotEqual(ConversionWordLanguage.Default, ConversionWordLanguage.InputLanguageOnly);
        Assert.NotEqual(ConversionWordLanguage.Default, ConversionWordLanguage.InputLanguageAndEnglish);
        Assert.NotEqual(ConversionWordLanguage.Default, ConversionWordLanguage.InterfaceInputAndEnglish);
    }

    [Fact]
    public void SharedServiceDoesNotMixConcurrentModesOrCultures()
    {
        Parallel.For(0, 64, index =>
        {
            var french = index % 2 == 0;
            var bilingual = index % 4 < 2;
            var result = _service.ConvertQuery("1 m in cm", CultureInfo.GetCultureInfo(french ? "fr-FR" : "en-US"),
                bilingual ? ConversionWordLanguage.InputLanguageAndEnglish : ConversionWordLanguage.InputLanguageOnly);
            Assert.Equal(!french || bilingual, result.Success);
        });
    }
}