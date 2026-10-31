using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.AppTests;

public class AutomaticLanguagePresentationTests
{
    private static readonly string[] LocaleNames =
    [
        "en-US", "cs-CZ", "de-DE", "es-ES", "fr-FR", "it-IT", "ja-JP", "ko-KR",
        "nl-NL", "pl-PL", "pt-BR", "pt-PT", "zh-CN", "zh-TW"
    ];

    public static IEnumerable<object[]> LanguagePairs =>
        from ui in LocaleNames
        from regional in LocaleNames
        select new object[] { ui, regional };

    [Theory]
    [MemberData(nameof(LanguagePairs))]
    public void DisplayedExamplesAndUnitNamesWorkWithEveryUiAndRegionPair(string uiName, string regionalName)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(regionalName);
            var ui = CultureInfo.GetCultureInfo(uiName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = ui;
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings { ParsingCulture = PreferredCulture.Default });
            var examples = page.GetItems().Where(item => item.Command is AnonymousCommand).ToArray();
            Assert.Equal(33, examples.Length);
            foreach (var example in examples)
            {
                var result = page.ConvertQuery(example.TextToSuggest);
                Assert.True(result.Success, $"{uiName}/{regionalName}: {example.TextToSuggest}: {result.ErrorMessage}");
            }
            var module = new ConversionService().Modules.Single(item => item.Category == "Length");
            var name = module.GetUnitMetadata("m", ui)!.DisplayName;
            var query = $"{1.5.ToString(culture)} {name} -> cm";
            var converted = page.ConvertQuery(query);
            Assert.True(converted.Success, $"{uiName}/{regionalName}: {query}: {converted.ErrorMessage}");
            Assert.Equal(150, converted.Output.Value, 8);

            page.SearchText = $"{1.5.ToString(culture)} {name[..Math.Min(3, name.Length)]}";
            if (!page.ConvertQuery(page.SearchText).Success)
            {
                var meter = Assert.Single(page.GetItems(), row => row.TextToSuggest == $"{1.5.ToString(culture)} m");
                Assert.EndsWith(" • " + name, meter.Subtitle);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData(ConversionWordLanguage.Default)]
    [InlineData(ConversionWordLanguage.InterfaceInputAndEnglish)]
    public void FrenchUiKeepsRegionalNumbersAndAcceptsAllThreeLanguages(ConversionWordLanguage mode)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var settings = new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.Default,
                ConversionWordLanguage = mode
            };
            var page = new UnitConverterExtensionPage(settings);
            Assert.Contains("1 m en ft", page.PlaceholderText);
            foreach (var query in new[] { "1,5 mètres en pieds", "1,5 metry na stopy", "1.5 meters to feet" })
            {
                page.SearchText = query;
                var row = page.GetItems()[0];
                Assert.Equal("Longueur • pied", row.Subtitle);
                Assert.Contains(',', row.Title);
                Assert.Equal(row.Title, Assert.IsType<CopyTextCommand>(row.Command).Text);
            }
            settings.ConversionWordLanguage = ConversionWordLanguage.InputLanguageAndEnglish;
            Assert.False(page.ConvertQuery("1 m en ft").Success);
            Assert.True(page.ConvertQuery("1 m to ft").Success);
            settings.ConversionWordLanguage = ConversionWordLanguage.InputLanguageOnly;
            Assert.False(page.ConvertQuery("1 m to ft").Success);
            Assert.True(page.ConvertQuery("1 metr na stopy").Success);
            settings.ParsingCulture = PreferredCulture.ForceEnglishCulture;
            settings.ConversionWordLanguage = mode;
            Assert.False(page.ConvertQuery("1 m en ft").Success);
            Assert.True(page.ConvertQuery("1 m to ft").Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData(ConversionWordLanguage.Default)]
    [InlineData(ConversionWordLanguage.InterfaceInputAndEnglish)]
    public void AmbiguousRootQueryOpensExplicitChoicesWithoutCopyingAGuess(ConversionWordLanguage mode)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("pt-PT");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-ES");
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.Default,
                ConversionWordLanguage = mode
            });
            var fallback = new FallbackConversionItem(page);
            fallback.UpdateQuery("100 grado -> deg");
            Assert.Equal(ConverterText.Get("Choose a unit interpretation.").TrimEnd('.'), fallback.Title);
            Assert.Equal("100 grado -> deg", fallback.Subtitle);
            Assert.Empty(page.SearchText);
            var open = Assert.IsAssignableFrom<IListPage>(fallback.Command);
            Assert.Equal("100 grado -> deg", open.SearchText);
            Assert.NotSame(page, open);
            Assert.Equal("com.jpsoftworks.cmdpal.unitconverter.fallback", Assert.IsAssignableFrom<IFallbackCommandItem2>(fallback).Id);
            Assert.DoesNotContain(open.GetItems(), row => row.Command is CopyTextCommand);
            var choices = open.GetItems().Where(row => row.Command is AnonymousCommand).ToArray();
            Assert.Equal(2, choices.Length);
            Assert.Empty(page.SearchText);
            foreach (var query in choices.Select(choice => choice.TextToSuggest).ToArray())
            {
                fallback.UpdateQuery(query);
                Assert.IsType<CopyTextCommand>(fallback.Command);
            }
            fallback.UpdateQuery("open settings");
            Assert.Empty(fallback.Title);
            Assert.IsType<NoOpCommand>(fallback.Command);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}