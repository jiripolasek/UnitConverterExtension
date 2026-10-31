using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.AppTests;

public class SectionHeaderTests
{
    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void EmptyPageGroupsExamplesAndRestoresThemAfterAQuery(string cultureName)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings());
            var rows = page.GetItems().Select(Assert.IsType<ListItem>).ToArray();
            Assert.IsType<HelpPage>(rows[0].Command);
            Assert.Empty(rows[0].Section);
            var examples = rows.Where(row => row.Command is AnonymousCommand).ToArray();
            Assert.Equal(33, examples.Length);
            var expectedSections = cultureName == "fr-FR"
                ? new[] { "Conversions du quotidien", "Sciences et ingénierie", "Données et informatique", "Unités composées et expressions" }
                : new[] { "Everyday conversions", "Science and engineering", "Data and computing", "Mixed units and expressions" };
            var headers = rows.Where(row => row.Command is null).ToArray();
            Assert.Equal(expectedSections, headers.Select(row => row.Section));
            Assert.Equal(1 + examples.Length + headers.Length, rows.Length);
            Assert.All(headers, row =>
            {
                Assert.Equal(row.Section, row.Title);
                Assert.Empty(row.MoreCommands);
            });
            Assert.All(rows.Where(row => row.Command is not null), row => Assert.Empty(row.Section));

            foreach (var example in examples)
            {
                Assert.Equal(CommandResultKind.KeepOpen, Assert.IsType<AnonymousCommand>(example.Command).Invoke().Kind);
                Assert.True(page.ConvertQuery(page.SearchText).Success, page.SearchText);
                Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command);
            }

            page.SearchText = "  ";
            Assert.Equal(rows.Select(row => (row.Title, row.Section, row.Command is null)),
                page.GetItems().Select(Assert.IsType<ListItem>).Select(row => (row.Title, row.Section, row.Command is null)));
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Theory]
    [InlineData("1 m", "en-US", "More conversions")]
    [InlineData("1 m", "fr-FR", "Autres conversions")]
    [InlineData("1 m to ft", "en-US", "More conversions")]
    [InlineData("1 m to ft", "fr-FR", "Autres conversions")]
    public void MoreConversionsFollowTheResultAndKeepCopyActions(string query, string cultureName, string section)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = query };
            var rows = page.GetItems().Select(Assert.IsType<ListItem>).ToArray();
            Assert.Equal(page.ConvertQuery(query).FormattedResult, rows[0].Title);
            Assert.Empty(rows[0].Section);
            var header = Assert.Single(rows, row => row.Section == section);
            Assert.Null(header.Command);
            Assert.Equal(section, header.Title);
            Assert.Equal(5, rows.SkipWhile(row => row != header).Skip(1).Count());
            var headers = rows.Where(row => row.Command is null).ToArray();
            Assert.Equal(query == "1 m" ? 2 : 1, headers.Length);
            Assert.All(headers, row => Assert.False(string.IsNullOrWhiteSpace(row.Section)));
            var conversions = rows.Where(row => row.Command is not null).ToArray();
            Assert.Equal(conversions.Length, conversions.Select(row => row.Title).Distinct().Count());
            Assert.All(conversions, row =>
            {
                Assert.Empty(row.Section);
                Assert.Equal(row.Title, Assert.IsType<CopyTextCommand>(row.Command).Text);
                Assert.Equal(2, row.MoreCommands.OfType<ICommandContextItem>().Count(item => item.Command is CopyTextCommand));
            });
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void NoAdditionalConversionsMeansNoAdditionalHeader()
    {
        var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings()) { SearchText = "0 delta C" };
        var header = Assert.Single(page.GetItems(), row => row.Command is null);
        Assert.Equal(ConverterText.Get("Suggested conversions"), header.Section);
    }
}