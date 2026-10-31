using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.AppTests;

public class ConversionWordLanguageTests
{
    [Theory]
    [InlineData("1,5 meters to feet")]
    [InlineData("1.5 meters in feet")]
    [InlineData("1,5 metry na stopy")]
    public void DefaultAcceptsEnglishAndRegionalQueriesWithEnglishUi(string query)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("cs-CZ");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.UseCurrentCulture
            });
            var result = page.ConvertQuery(query);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(1.5 / 0.3048, result.Output.Value, 8);
            Assert.Contains("1 m to ft", page.PlaceholderText);
            page.SearchText = query;
            var row = page.GetItems()[0];
            Assert.Equal("Length • Foot", row.Subtitle);
            Assert.Contains(',', row.Title);
            Assert.Equal(row.Title, Assert.IsType<CopyTextCommand>(row.Command).Text);
            var fallback = new FallbackConversionItem(page);
            fallback.UpdateQuery(query);
            Assert.Equal(row.Title, Assert.IsType<CopyTextCommand>(fallback.Command).Text);
            page.SearchText = "1,5 m to f";
            var completion = Assert.Single(page.GetItems(), item => item.TextToSuggest == "1,5 m to ft");
            Assert.Equal("Foot", completion.Subtitle);
            Assert.Contains(',', completion.Title);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void ChangingTheModeUpdatesRootResultsAndCompletionsWithoutRecreatingThePage()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var settings = new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.UseCurrentCulture,
                ConversionWordLanguage = ConversionWordLanguage.InputLanguageAndEnglish
            };
            var page = new UnitConverterExtensionPage(settings);
            var fallback = new FallbackConversionItem(page);
            fallback.UpdateQuery("1 m in cm");
            Assert.Equal("100 cm", Assert.IsType<CopyTextCommand>(fallback.Command).Text);

            settings.ConversionWordLanguage = ConversionWordLanguage.InputLanguageOnly;
            fallback.UpdateQuery("1 m in cm");
            Assert.Empty(fallback.Title);
            Assert.IsType<NoOpCommand>(fallback.Command);
            fallback.UpdateQuery("1 m en cm");
            Assert.Equal("100 cm", Assert.IsType<CopyTextCommand>(fallback.Command).Text);

            page.SearchText = "1 m en c";
            Assert.Contains(page.GetItems(), item => item.TextToSuggest == "1 m en cm");
            page.SearchText = "1 m in c";
            Assert.DoesNotContain(page.GetItems(), item => item.TextToSuggest == "1 m in cm");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void InputLanguageControlsWordsIndependentlyOfTheUiLanguage()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var settings = new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.ForceEnglishCulture,
                ConversionWordLanguage = ConversionWordLanguage.InputLanguageOnly
            };
            var page = new UnitConverterExtensionPage(settings);
            Assert.True(page.ConvertQuery("1 m in cm").Success);
            Assert.False(page.ConvertQuery("1 m en cm").Success);
            settings.ParsingCulture = PreferredCulture.UseCurrentCulture;
            Assert.False(page.ConvertQuery("1 m in cm").Success);
            Assert.True(page.ConvertQuery("1 m en cm").Success);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void ExamplesAndInputHintsStayUsableInStrictFrenchMode()
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.UseCurrentCulture,
                ConversionWordLanguage = ConversionWordLanguage.InputLanguageOnly
            });
            Assert.Contains("1 m en ft", page.PlaceholderText);
            Assert.True(page.ConvertQuery("1 m -> ft").Success);
            var examples = page.GetItems().Where(item => item.Command is AnonymousCommand).ToArray();
            Assert.Contains(examples, item => item.TextToSuggest == "1 m en ft");
            Assert.Equal(33, examples.Length);
            foreach (var example in examples)
            {
                Assert.True(page.ConvertQuery(example.TextToSuggest).Success, example.TextToSuggest);
                Assert.IsType<AnonymousCommand>(example.Command).Invoke();
                Assert.Equal(example.TextToSuggest, page.SearchText);
                Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }
}
