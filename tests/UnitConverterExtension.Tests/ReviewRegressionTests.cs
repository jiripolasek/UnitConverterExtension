using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class ReviewRegressionTests
{
    [Fact]
    public void RootOffersAnIsolatedPageUsingTheHostsSupportedPageContract()
    {
        var settings = new FallbackConversionTests.TestSettings();
        var page = new UnitConverterExtensionPage(settings);
        var fallback = new FallbackConversionItem(page);
        var updates = 0;
        page.ItemsChanged += (_, _) => updates++;
        fallback.UpdateQuery("5 km");
        var accuracyReads = settings.AccuracyReads;
        Assert.Empty(page.SearchText);
        Assert.Equal(0, updates);
        var open = Assert.Single(fallback.MoreCommands.OfType<ICommandContextItem>()
            .Select(item => item.Command).OfType<IListPage>());
        fallback.UpdateQuery("open settings");
        Assert.Empty(page.SearchText);
        Assert.NotSame(page, open);
        Assert.NotEqual(page.Id, open.Id);
        Assert.Equal("5 km", open.SearchText);
        Assert.IsAssignableFrom<IPage>(open);
        Assert.Equal(accuracyReads, settings.AccuracyReads);
        var rows = open.GetItems();
        Assert.True(settings.AccuracyReads > accuracyReads);
        Assert.Same(rows, open.GetItems());
        Assert.Contains(rows, item => item.Command is CopyTextCommand);
        Assert.IsType<UnitConverterExtensionPage>(open).SearchText = "2 m to cm";
        Assert.Equal("200 cm", Assert.IsType<CopyTextCommand>(open.GetItems()[0].Command).Text);
        Assert.Empty(page.SearchText);
        Assert.Equal(0, updates);
    }

    [Fact]
    public void AmbiguousDataRateUsesAPageCommandAndChoicesRemainActionable()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings());
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery("10 mbps");
        var open = Assert.IsAssignableFrom<IListPage>(fallback.Command);
        Assert.IsAssignableFrom<IPage>(open);
        Assert.Equal("10 mbps", open.SearchText);
        var choices = open.GetItems().Where(row => row.Command is AnonymousCommand).ToArray();
        Assert.Equal(2, choices.Length);
        foreach (var choice in choices)
        {
            Assert.Equal(CommandResultKind.KeepOpen, Assert.IsType<AnonymousCommand>(choice.Command).Invoke().Kind);
            Assert.Equal(choice.TextToSuggest, open.SearchText);
            Assert.IsType<CopyTextCommand>(open.GetItems()[0].Command);
        }
        Assert.Empty(page.SearchText);
    }

    [Theory]
    [InlineData(ConversionWordLanguage.Default)]
    [InlineData(ConversionWordLanguage.InterfaceInputAndEnglish)]
    public void HelpAliasesFollowChangedInputRestrictions(ConversionWordLanguage mode)
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
            var help = Assert.IsType<HelpPage>(page.GetItems().Single(row => row.Command is HelpPage).Command);
            Assert.Contains("`pied`", FootAliases(help));
            Assert.Contains("`stopa`", FootAliases(help));
            settings.ConversionWordLanguage = ConversionWordLanguage.InputLanguageOnly;
            Assert.DoesNotContain("`pied`", FootAliases(help));
            Assert.Contains("`stopa`", FootAliases(help));
            settings.ParsingCulture = PreferredCulture.ForceEnglishCulture;
            Assert.DoesNotContain("`stopa`", FootAliases(help));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    private static string FootAliases(HelpPage page)
    {
        var body = Assert.IsType<MarkdownContent>(Assert.Single(page.GetContent())).Body;
        return body.Split("- **ft** - ", 2)[1].Split('\n')[1];
    }
}