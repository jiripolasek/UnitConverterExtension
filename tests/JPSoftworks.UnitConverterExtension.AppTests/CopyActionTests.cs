using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.AppTests;

public class CopyActionTests
{
    [Theory]
    [InlineData("en-US", PreferredCulture.UseCurrentCulture)]
    [InlineData("fr-FR", PreferredCulture.UseCurrentCulture)]
    [InlineData("fr-FR", PreferredCulture.ForceEnglishCulture)]
    public void CopyActionsUseTheQueryCultureAndUnroundedValue(string cultureName, PreferredCulture preference)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            var culture = preference == PreferredCulture.ForceEnglishCulture ? CultureInfo.GetCultureInfo("en-US") : CultureInfo.CurrentCulture;
            var settings = new FallbackConversionTests.TestSettings { ParsingCulture = preference, Accuracy = AccuracyLevel.CommonUse };
            var page = new UnitConverterExtensionPage(settings) { SearchText = "1/3 m->ft" };
            var result = page.ConvertQuery(page.SearchText);
            var row = page.GetItems()[0];
            Assert.Equal(result.FormattedResult, Assert.IsType<CopyTextCommand>(row.Command).Text);
            var commands = GetCopies(row.MoreCommands);
            Assert.Equal(2, commands.Length);
            Assert.Equal(1.09.ToString(culture), commands[0].Text);
            Assert.Equal(result.Output.Value, double.Parse(commands[1].Text.Split(' ')[0], culture));
            Assert.EndsWith(" ft", commands[1].Text);
            Assert.NotEqual(result.FormattedResult, commands[1].Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("1 m² to ft²", " ft²")]
    [InlineData("1 m³ to l", " L")]
    [InlineData("1 m² to ha", " ha")]
    [InlineData("1e-20 m to ft", " ft")]
    public void FullPrecisionCopyKeepsTheCorrectUnitAndTinyValues(string query, string suffix)
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = query };
        var result = page.ConvertQuery(query);
        var copy = GetCopies(page.GetItems()[0].MoreCommands)[1];
        Assert.EndsWith(suffix, copy.Text);
        Assert.Equal(result.Output.Value, double.Parse(copy.Text.Split(' ')[0], CultureInfo.GetCultureInfo("en-US")));
    }

    [Fact]
    public void SuggestionsAndRootResultsExposeTheSameCopyActions()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "1 m" };
        var rows = page.GetItems();
        Assert.Empty(Assert.IsType<ListItem>(rows[0]).Section);
        var suggestionCount = Math.Min(5, page.ConvertQuery(page.SearchText).Suggestions.Length);
        var suggestionRows = rows.Where(row => row.Command is CopyTextCommand).Skip(1).Take(suggestionCount).ToArray();
        Assert.Equal(suggestionCount, suggestionRows.Length);
        Assert.NotEmpty(suggestionRows);
        Assert.All(suggestionRows, row => Assert.Empty(Assert.IsType<ListItem>(row).Section));
        Assert.All(rows.Where(row => row.Command is CopyTextCommand), row => Assert.Equal(2, GetCopies(row.MoreCommands).Length));
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery("1 m");
        Assert.Equal(GetCopies(suggestionRows[0].MoreCommands).Select(copy => copy.Text),
            GetCopies(fallback.MoreCommands).Select(copy => copy.Text));
    }

    [Fact]
    public void CopyActionLabelsFollowTheUiLanguage()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "1 m to cm" };
            var commands = GetCopies(page.GetItems()[0].MoreCommands);
            Assert.Equal("Copier le nombre", commands[0].Name);
            Assert.Equal("Copier avec toute la précision", commands[1].Name);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void CompoundResultsPreserveAllUnitsInFullPrecisionCopies()
    {
        var settings = new FallbackConversionTests.TestSettings { Accuracy = AccuracyLevel.CommonUse };
        var page = new UnitConverterExtensionPage(settings) { SearchText = "180 cm to ft in" };
        var row = page.GetItems()[0];
        Assert.Equal("5 ft 10.9 in", row.Title);
        var copies = GetCopies(row.MoreCommands);
        Assert.Contains("ft", copies[0].Name);
        Assert.Contains(" ft ", copies[1].Text);
        Assert.EndsWith(" in", copies[1].Text);
        Assert.NotEqual(row.Title, copies[1].Text);
        Assert.Equal(180, page.ConvertQuery($"{copies[1].Text} to cm").Output.Value, 10);
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery(page.SearchText);
        Assert.Equal(row.Title, fallback.Title);
        Assert.Equal(copies.Select(copy => copy.Text), GetCopies(fallback.MoreCommands).Select(copy => copy.Text));
    }

    [Theory]
    [InlineData("100 Mbps to MB/s", "12.5 MB/s")]
    [InlineData("20 km/L to L/100km", "5 L/100km")]
    [InlineData("10 delta C to delta F", "18 Δ°F")]
    [InlineData("60 rpm to Hz", "1 Hz")]
    [InlineData("1 kN to N", "1000 N")]
    [InlineData("100 N·cm to Nm", "1 N·m")]
    [InlineData("1 ft/s^2 to in/s2", "12 in/s²")]
    [InlineData("1 g/cm3 to kg/m^3", "1000 kg/m³")]
    [InlineData("3.3 V to mV", "3300 mV")]
    [InlineData("500 mA to A", "0.5 A")]
    [InlineData("4.7 kohm to ohm", "4700 Ω")]
    [InlineData("12 km/h to min/km", "5:00 min/km")]
    public void NewCategoriesUseThePageAndFallbackCopyActions(string query, string expected)
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = query };
        var row = page.GetItems()[0];
        Assert.Equal(expected, row.Title);
        Assert.Equal(expected, Assert.IsType<CopyTextCommand>(row.Command).Text);
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery(query);
        Assert.Equal(row.Title, fallback.Title);
        Assert.Equal(GetCopies(row.MoreCommands).Select(copy => copy.Text), GetCopies(fallback.MoreCommands).Select(copy => copy.Text));
    }

    [Fact]
    public void PaceCopiesDistinguishClockDisplayFromDecimalMinutes()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "5:30 min/km" };
        var row = page.GetItems()[0];
        Assert.Equal("5:30 min/km", Assert.IsType<CopyTextCommand>(row.Command).Text);
        var copies = GetCopies(row.MoreCommands);
        Assert.Contains("min/km", copies[0].Name);
        Assert.Equal("5.5", copies[0].Text);
        Assert.Equal("5.5 min/km", copies[1].Text);
        Assert.Equal(330, page.ConvertQuery($"{copies[1].Text} to s/km").Output.Value, 8);
    }

    private static CopyTextCommand[] GetCopies(IEnumerable<IContextItem> items) =>
        items.OfType<ICommandContextItem>().Select(item => item.Command).OfType<CopyTextCommand>().ToArray();
}