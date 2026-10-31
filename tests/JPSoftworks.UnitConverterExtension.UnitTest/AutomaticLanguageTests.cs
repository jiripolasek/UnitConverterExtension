using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class AutomaticLanguageTests
{
    private static readonly CultureInfo Czech = CultureInfo.GetCultureInfo("cs-CZ");

    [Theory]
    [InlineData("fr-FR", "1,5 mètres en pieds")]
    [InlineData("de-DE", "1,5 Meter in Fuß")]
    [InlineData("es-ES", "1,5 metros a pies")]
    [InlineData("ja-JP", "1,5メートルをフィートに")]
    [InlineData("ko-KR", "1,5미터를피트로")]
    [InlineData("zh-CN", "1,5米转英尺")]
    [InlineData("zh-TW", "1,5公尺轉英尺")]
    public void DefaultAndExplicitAllLanguagesAcceptUiRegionalEnglishAndMixedVocabulary(string uiName, string query)
    {
        var service = new ConversionService(null, null, CultureInfo.GetCultureInfo(uiName));
        foreach (var input in new[] { query, "1,5 metry na stopy", "1.5 meters to feet" })
        {
            foreach (var mode in new[] { ConversionWordLanguage.Default, ConversionWordLanguage.InterfaceInputAndEnglish })
            {
                var result = service.ConvertQuery(input, Czech, mode);
                Assert.True(result.Success, $"{uiName}: {input}: {result.ErrorMessage}");
                Assert.Equal("ft", result.Output.Unit.Unit);
                Assert.Equal(1.5 / 0.3048, result.Output.Value, 8);
                Assert.Contains(',', result.FormattedResult);
            }
        }

        var mixed = new ConversionService(null, null, CultureInfo.GetCultureInfo("fr-FR"))
            .ConvertQuery("1,5 mètre na stopy", Czech, ConversionWordLanguage.InterfaceInputAndEnglish);
        Assert.True(mixed.Success, mixed.ErrorMessage);
        Assert.Equal(1.5 / 0.3048, mixed.Output.Value, 8);
    }

    [Theory]
    [InlineData(ConversionWordLanguage.InputLanguageOnly)]
    [InlineData(ConversionWordLanguage.InputLanguageAndEnglish)]
    public void ExplicitModesKeepUiVocabularyOutOfTheInputLanguage(ConversionWordLanguage mode)
    {
        var service = new ConversionService(null, null, CultureInfo.GetCultureInfo("fr-FR"));
        Assert.False(service.ConvertQuery("1 m en cm", Czech, mode).Success);
        Assert.False(service.ConvertQuery("1 pied -> cm", Czech, mode).Success);
        Assert.True(service.ConvertQuery("1 metr na cm", Czech, mode).Success);
    }

    [Theory]
    [InlineData("es-ES", "pt-PT", "100 grado -> deg", "100 deg -> deg", "100 grad -> deg")]
    [InlineData("de-DE", "cs-CZ", "100 Grad -> deg", "100 deg -> deg", "100 grad -> deg")]
    public void ConflictingTranslatedNamesRequireAnExplicitMeaning(string uiName, string regionalName, string query, string degrees, string gradians)
    {
        var service = new ConversionService(null, null, CultureInfo.GetCultureInfo(uiName));
        var culture = CultureInfo.GetCultureInfo(regionalName);
        var result = service.ConvertQuery(query, culture);
        Assert.False(result.Success);
        Assert.True(result.IsIncomplete);
        Assert.Contains(result.OtherInterpretations, item => item.Query == degrees);
        Assert.Contains(result.OtherInterpretations, item => item.Query == gradians);
        Assert.Equal(100, service.ConvertQuery(degrees, culture).Output.Value, 8);
        Assert.Equal(90, service.ConvertQuery(gradians, culture).Output.Value, 8);
    }

    [Theory]
    [InlineData("fr-FR", "en-GB", 4.54609)]
    [InlineData("cs-CZ", "en-GB", 4.54609)]
    [InlineData("en-GB", "cs-CZ", 3.785411784)]
    public void VolumeAutoUsesTheRegionRegardlessOfUiOrConnectorLanguage(string uiName, string regionalName, double liters)
    {
        var service = new ConversionService(new DefaultUnitPreferenceProvider(VolumeSystemPreference.Auto), null, CultureInfo.GetCultureInfo(uiName));
        var culture = CultureInfo.GetCultureInfo(regionalName);
        foreach (var query in new[] { "1 gallon to l", service.FormatConversionQuery("1 gallon", "l", culture) })
        {
            var result = service.ConvertQuery(query, culture);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(liters, result.Output.Value, 8);
        }
        Assert.Equal(3.785411784, service.ConvertQuery("1 us gal -> l", culture).Output.Value, 8);
    }

    [Fact]
    public void CustomRegionalNumberFormatsArePreserved()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = ".";
        var service = new ConversionService(null, null, CultureInfo.GetCultureInfo("fr-FR"));
        var result = service.ConvertQuery("1,5 mètres en cm", culture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(150, result.Output.Value, 8);
        Assert.Equal(1234, service.ConvertQuery("1.234 mètres en m", culture).Output.Value, 8);
    }

    [Fact]
    public void SharedCachesKeepDifferentUiVocabulariesSeparate()
    {
        var french = new ConversionService(null, null, CultureInfo.GetCultureInfo("fr-FR"));
        var german = new ConversionService(null, null, CultureInfo.GetCultureInfo("de-DE"));
        Parallel.For(0, 24, _ =>
        {
            Assert.True(french.ConvertQuery("1 m en cm", Czech).Success);
            Assert.False(german.ConvertQuery("1 m en cm", Czech).Success);
            Assert.True(german.ConvertQuery("1 m zu cm", Czech).Success);
            Assert.False(french.ConvertQuery("1 m zu cm", Czech).Success);
        });
    }

    [Theory]
    [InlineData(ConversionWordLanguage.Default)]
    [InlineData(ConversionWordLanguage.InterfaceInputAndEnglish)]
    public void NativeCompletionsKeepTheLanguageTheUserTyped(ConversionWordLanguage mode)
    {
        var service = new ConversionService(null, null, CultureInfo.GetCultureInfo("ja-JP"));
        var culture = CultureInfo.GetCultureInfo("ko-KR");
        foreach (var query in new[] { "1미터를", "1メートルを" })
        {
            var result = service.ConvertQuery(query, culture, mode);
            Assert.True(result.IsIncomplete, result.ErrorMessage);
            Assert.NotEmpty(result.Completions);
            Assert.All(result.Completions, completion =>
            {
                Assert.StartsWith(query, completion.Query);
                Assert.True(service.ConvertQuery(completion.Query, culture, mode).Success, completion.Query);
            });
        }
        Assert.True(service.ConvertQuery("1米をftに", CultureInfo.GetCultureInfo("zh-CN"), mode).Success);
    }
}