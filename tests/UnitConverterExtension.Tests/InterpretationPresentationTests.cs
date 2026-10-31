using System.Globalization;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class InterpretationPresentationTests
{
    [Theory]
    [InlineData("en-US", "Other interpretations")]
    [InlineData("fr-FR", "Autres interprétations")]
    public void ExactResultRemainsFirstAndAlternativesCanReplaceTheQuery(string cultureName, string heading)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "10 mN to N" };
            var rows = page.GetItems();
            Assert.Equal("0.01 N", Assert.IsType<CopyTextCommand>(rows[0].Command).Text);
            Assert.Equal(heading, rows[1].Section);
            Assert.Null(rows[1].Command);
            var alternative = Assert.Single(rows, row => row.TextToSuggest == "10 MN to N");
            Assert.Equal("10 MN to N", alternative.Title);
            Assert.Equal("10000000 N", alternative.Subtitle);
            Assert.Equal(CommandResultKind.KeepOpen, Assert.IsType<AnonymousCommand>(alternative.Command).Invoke().Kind);
            Assert.Equal("10 MN to N", page.SearchText);
            Assert.Equal("10000000 N", Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command).Text);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void PintOffersASelectableDtpPointInterpretation()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "1 pt" };
        var rows = page.GetItems();
        Assert.IsType<CopyTextCommand>(rows[0].Command);
        var alternative = Assert.Single(rows, row => row.TextToSuggest == "1 dtp pt");
        Assert.Equal("1 dtp pt", alternative.Title);
        Assert.Equal(CommandResultKind.KeepOpen, Assert.IsType<AnonymousCommand>(alternative.Command).Invoke().Kind);
        Assert.Equal("1 dtp pt", page.SearchText);
        Assert.Equal("1 pt (DTP)", Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command).Text);
    }

    [Fact]
    public void AmbiguousCasingOffersChoicesWithoutACopyableGuess()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "10 mn to N" };
        var rows = page.GetItems();
        Assert.Null(rows[0].Command);
        Assert.DoesNotContain(rows, row => row.Command is CopyTextCommand);
        Assert.Contains(rows, row => row.TextToSuggest == "10 mN to N");
        Assert.Contains(rows, row => row.TextToSuggest == "10 MN to N");
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery("10 mn to N");
        Assert.False(string.IsNullOrWhiteSpace(fallback.Title));
        var open = Assert.IsAssignableFrom<IListPage>(fallback.Command);
        Assert.NotSame(page, open);
        Assert.Equal("10 mn to N", open.SearchText);
        Assert.Contains(open.GetItems(), row => row.TextToSuggest == "10 mN to N");
        Assert.Contains(open.GetItems(), row => row.TextToSuggest == "10 MN to N");
    }

    [Fact]
    public void UnambiguousCasingWorksForBothThePageAndRootFallback()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "10 LY to km" };
        var result = page.ConvertQuery("10 ly to km");
        Assert.Equal(result.FormattedResult, Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command).Text);
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery("10 LY to km");
        Assert.Equal(result.FormattedResult, fallback.Title);
        Assert.DoesNotContain(page.GetItems(), row => row.Command is AnonymousCommand);
    }

    [Fact]
    public void ExactFallbackKeepsItsConversionWhenAlternativesExist()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings());
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery("10 mN to N");
        Assert.Equal("0.01 N", fallback.Title);
        Assert.Equal("0.01 N", Assert.IsType<CopyTextCommand>(fallback.Command).Text);
    }

    [Fact]
    public void AnInvalidExactMeaningKeepsItsErrorAboveTheAlternatives()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "1e308 MN to N" };
        var rows = page.GetItems();
        Assert.IsType<NoOpCommand>(rows[0].Command);
        Assert.False(string.IsNullOrWhiteSpace(rows[0].Subtitle));
        Assert.Null(rows[1].Command);
        Assert.Contains(rows, row => row.TextToSuggest == "1e308 mN to N");
        Assert.DoesNotContain(rows, row => row.Command is CopyTextCommand);
    }
}