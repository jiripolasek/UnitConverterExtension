using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class LocalizationTests
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    [Theory]
    [InlineData("1 pied 2 pouces en centimètres", 35.56)]
    [InlineData("1 pied 2 pouces in centimètres", 35.56)]
    [InlineData("1,5 mètres in centimètres", 150)]
    [InlineData("1,5 mètres IN centimètres", 150)]
    [InlineData("1 pied 2 in in centimètres", 35.56)]
    [InlineData("1 heure 30 minutes vers secondes", 5400)]
    [InlineData("(1,5 + 2,5) mètres en centimètres", 400)]
    [InlineData("2 mètres carrés en centimètres carrés", 20000)]
    [InlineData("1 mètre cube en litres", 1000)]
    [InlineData("1 mégaoctet en mégabits", 8)]
    [InlineData("1 livre par pouce carré en psi", 1)]
    [InlineData("1 cuillère à café en millilitres", 4.92892159375)]
    [InlineData("1 cuillère à soupe en cuillères à café", 3)]
    [InlineData("100 degrés Celsius en degrés Fahrenheit", 212)]
    public void FrenchQueriesSupportPluralsAndExpressions(string query, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, French, ConversionWordLanguage.InputLanguageAndEnglish);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
    }

    [Fact]
    public void EveryAdvertisedUnitHasFrenchNamesThatResolveToItsStableId()
    {
        var service = new ConversionService();
        foreach (var module in service.Modules)
        {
            var english = module.GetAllUnitMetadata(CultureInfo.GetCultureInfo("en-US"));
            var french = module.GetAllUnitMetadata(French);
            Assert.Equal(english.Keys.Order(StringComparer.Ordinal), french.Keys.Order(StringComparer.Ordinal));
            foreach (var id in english.Keys)
            {
                var names = ConverterText.GetUnitNames(module.Category, id, French);
                Assert.NotEmpty(names);
                Assert.Equal(names[0], french[id].DisplayName);
                foreach (var name in names)
                {
                    Assert.True(module.TryNormalizeUnit(name.ToUpper(French), French, out var normalized), name);
                    Assert.Equal(id, normalized);
                    var result = service.ConvertQuery($"1 {name} en {id}", French);
                    Assert.True(result.Success, $"{name}: {result.ErrorMessage}");
                    Assert.Equal(1, result.Output.Value, 8);
                }
            }
        }
    }

    [Theory]
    [InlineData(VolumeSystemPreference.US, 473.176473)]
    [InlineData(VolumeSystemPreference.Imperial, 568.26125)]
    public void FrenchUnqualifiedVolumeRespectsPreferences(VolumeSystemPreference preference, double expected)
    {
        var service = new ConversionService(new DefaultUnitPreferenceProvider(preference));
        var result = service.ConvertQuery("1 pinte en millilitres", French);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 6);
    }

    [Theory]
    [InlineData("en-US", "Help", "Meter")]
    [InlineData("fr-FR", "Aide", "mètre")]
    [InlineData("fr-CA", "Aide", "mètre")]
    [InlineData("de-DE", "Hilfe", "Meter")]
    [InlineData("hu-HU", "Help", "Meter")]
    public void ResourcesFallBackByLanguage(string cultureName, string help, string meter)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        Assert.Equal(help, ConverterText.Get("Help", culture));
        var result = new ConversionService().ConvertQuery("1 ft->m", culture);
        Assert.Equal(meter, result.Output.Unit.Metadata!.DisplayName);
    }

    [Fact]
    public void SharedServiceSupportsConcurrentCultureLookups()
    {
        var service = new ConversionService();
        var cultures = new[] { "en-US", "fr-FR", "fr-CA", "de-DE" };
        Parallel.For(0, 128, index =>
        {
            var result = service.ConvertQuery("1 ft->m", CultureInfo.GetCultureInfo(cultures[index % cultures.Length]));
            Assert.True(result.Success);
            Assert.Equal(0.3048, result.Output.Value, 8);
        });
    }

    [Fact]
    public void FrenchErrorsAndSuggestionsAreLocalized()
    {
        var service = new ConversionService();
        Assert.StartsWith("Unité inconnue", service.ConvertQuery("1 inconnue", French).ErrorMessage);
        var result = service.ConvertQuery("1 mètre", French);
        var feet = Assert.Single(result.Suggestions, item => item.Output.Unit.Unit == "ft");
        Assert.Equal("pied", feet.Output.Unit.Metadata!.DisplayName);
        Assert.Contains(',', feet.FormattedResult);
    }
}