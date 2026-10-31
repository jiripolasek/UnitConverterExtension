using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class CompletionPresentationTests
{
    [Theory]
    [InlineData("en-US", "10 N to", "10 N to kN", "0.01 kN", "Kilonewton", "Choose a compatible target unit")]
    [InlineData("en-US", "10 N in", "10 N in kN", "0.01 kN", "Kilonewton", "Choose a compatible target unit")]
    [InlineData("fr-FR", "10 N en", "10 N en kN", "0,01 kN", "kilonewton", "Choisissez une unité cible compatible")]
    [InlineData("fr-FR", "10 N in", "10 N in kN", "0,01 kN", "kilonewton", "Choisissez une unité cible compatible")]
    public void TargetChoicesShowPreviewsUnderANoninteractiveHeading(
        string cultureName, string query, string completedQuery, string preview, string unitName, string heading)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var settings = new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.UseCurrentCulture,
                ConversionWordLanguage = ConversionWordLanguage.InputLanguageAndEnglish
            };
            var page = new UnitConverterExtensionPage(settings) { SearchText = query };
            var rows = page.GetItems();
            Assert.Null(rows[0].Command);
            Assert.Equal(heading, rows[0].Section);
            Assert.Equal(heading, rows[0].Title);
            Assert.DoesNotContain(rows, row => row.Command is NoOpCommand);
            var choices = rows.Where(row => row.Command is AnonymousCommand).ToArray();
            Assert.Equal(7, choices.Length);
            Assert.DoesNotContain(choices, row => row.TextToSuggest.EndsWith(" N", StringComparison.Ordinal));
            var choice = Assert.Single(choices, row => row.TextToSuggest == completedQuery);
            Assert.Equal(preview, choice.Title);
            Assert.Equal(unitName, choice.Subtitle);
            Assert.All(choices, row => Assert.Equal(page.ConvertQuery(row.TextToSuggest).FormattedResult, row.Title));
            Assert.Equal(CommandResultKind.KeepOpen, Assert.IsType<AnonymousCommand>(choice.Command).Invoke().Kind);
            Assert.Equal(completedQuery, page.SearchText);
            Assert.Equal(preview, Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command).Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Fact]
    public void SourceChoicesKeepTheirQueriesAndCategoryLabels()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "10 kilogr" };
        var rows = page.GetItems();
        Assert.Null(rows[0].Command);
        Assert.False(rows[0].Section.EndsWith('.'));
        var choice = Assert.Single(rows, row => row.TextToSuggest == "10 kg");
        Assert.Equal("10 kg", choice.Title);
        Assert.Contains(" • ", choice.Subtitle);
    }

    [Fact]
    public void IncompatibleTargetsRetainTheErrorAndOfferValuePreviews()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "10 N to s" };
        var rows = page.GetItems();
        Assert.IsType<NoOpCommand>(rows[0].Command);
        Assert.False(string.IsNullOrWhiteSpace(rows[0].Subtitle));
        var choices = rows.Where(row => row.Command is AnonymousCommand).ToArray();
        Assert.NotEmpty(choices);
        Assert.All(choices, row => Assert.Equal(page.ConvertQuery(row.TextToSuggest).FormattedResult, row.Title));
    }
}