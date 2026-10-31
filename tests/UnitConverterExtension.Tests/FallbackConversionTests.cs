using System.Globalization;
using System.Reflection;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class FallbackConversionTests
{
    internal sealed class TestSettings : ISettingsManager
    {
        public VolumeSystemPreference VolumeSystemPreference { get; set; } = VolumeSystemPreference.US;
        public PreferredCulture ParsingCulture { get; set; } = PreferredCulture.ForceEnglishCulture;
        public ConversionWordLanguage ConversionWordLanguage { get; set; } = ConversionWordLanguage.Default;
        private AccuracyLevel _accuracy = AccuracyLevel.ExtremelyPrecise;
        public int AccuracyReads { get; private set; }
        public AccuracyLevel Accuracy
        {
            get { AccuracyReads++; return _accuracy; }
            set => _accuracy = value;
        }
    }

    [Theory]
    [InlineData("1 ft 2 in to cm")]
    [InlineData("10m in ft")]
    [InlineData("1 ft 2 in in cm")]
    [InlineData("180 cm in ft in")]
    [InlineData("(2 + 3) m to ft")]
    [InlineData("1e-10 m to cm")]
    public void RootQueryUsesTheSameResultAndCopyTextAsThePage(string query)
    {
        var page = new UnitConverterExtensionPage(new TestSettings());
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery(query);
        Assert.Equal(page.ConvertQuery(query).FormattedResult, fallback.Title);
        Assert.Equal(fallback.Title, Assert.IsType<CopyTextCommand>(fallback.Command).Text);
        Assert.Equal(query, fallback.Subtitle);
        Assert.Empty(page.SearchText);
        var open = fallback.MoreCommands.OfType<ICommandContextItem>().Select(item => item.Command).OfType<IListPage>().Single();
        Assert.NotSame(page, open);
        Assert.Equal(query, open.SearchText);
        Assert.Equal(fallback.Title, Assert.IsType<CopyTextCommand>(open.GetItems()[0].Command).Text);
        Assert.Empty(page.SearchText);
    }

    [Fact]
    public void FallbackRuntimeClassNameExposesTheIdentityInterface()
    {
        var attribute = typeof(FallbackConversionItem).GetCustomAttribute<WinRT.WinRTRuntimeClassNameAttribute>(inherit: false);
        Assert.NotNull(attribute);
        Assert.Equal(typeof(IFallbackCommandItem2).FullName, attribute.RuntimeClassName);
    }

    [Fact]
    public void FallbackIdentityRemainsStableAcrossQueryUpdates()
    {
        const string expectedId = "com.jpsoftworks.cmdpal.unitconverter.fallback";
        var fallback = new FallbackConversionItem(new UnitConverterExtensionPage(new TestSettings()));
        var identity = Assert.IsAssignableFrom<IFallbackCommandItem2>(fallback);
        Assert.Equal(expectedId, identity.Id);

        foreach (var query in new[] { "1 m to cm", "open settings", "1 ft 2 in to cm", "10 mbps" })
        {
            fallback.UpdateQuery(query);
            Assert.Equal(expectedId, identity.Id);
            Assert.Equal(expectedId, fallback.Command!.Id);
        }
    }

    [Fact]
    public void ImplicitRootQueryShowsAConversionSuggestion()
    {
        var page = new UnitConverterExtensionPage(new TestSettings());
        var fallback = new FallbackConversionItem(page);
        fallback.UpdateQuery("1 m");
        Assert.Equal(page.ConvertQuery("1 m").Suggestions[0].FormattedResult, fallback.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1 m to")]
    [InlineData("open settings")]
    [InlineData("1 kg to m")]
    public void UnrelatedOrIncompleteQueriesClearThePreviousFallback(string query)
    {
        var fallback = new FallbackConversionItem(new UnitConverterExtensionPage(new TestSettings()));
        fallback.UpdateQuery("1 m to cm");
        fallback.UpdateQuery(query);
        Assert.Empty(fallback.Title);
        Assert.Empty(fallback.Subtitle);
        Assert.IsType<NoOpCommand>(fallback.Command);
        Assert.Empty(fallback.MoreCommands);
    }

    [Fact]
    public void RootQueryHonorsFrenchCultureAndChangedSettings()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var settings = new TestSettings { ParsingCulture = PreferredCulture.UseCurrentCulture };
            var page = new UnitConverterExtensionPage(settings);
            var fallback = new FallbackConversionItem(page);
            fallback.UpdateQuery("1 pinte en litres");
            var us = fallback.Title;
            Assert.Contains(',', us);
            settings.VolumeSystemPreference = VolumeSystemPreference.Imperial;
            fallback.UpdateQuery("1 pinte en litres");
            Assert.NotEqual(us, fallback.Title);
            Assert.Equal(page.ConvertQuery("1 pinte en litres").FormattedResult, fallback.Title);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("", "1 m to ft")]
    [InlineData("1 m to c", "1 m to cm")]
    [InlineData("10 mN to N", "10 MN to N")]
    public void SelectingAQueryNotifiesTheHostAndKeepsThePageOpen(string initialQuery, string selectedQuery)
    {
        var page = new UnitConverterExtensionPage(new TestSettings()) { SearchText = initialQuery };
        var notifiedQueries = new List<string>();
        page.PropChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(page.SearchText))
            {
                notifiedQueries.Add(page.SearchText);
            }
        };
        var choice = Assert.Single(page.GetItems(), item => item.TextToSuggest == selectedQuery);
        var result = Assert.IsType<AnonymousCommand>(choice.Command).Invoke();
        Assert.Equal(CommandResultKind.KeepOpen, result.Kind);
        Assert.Equal(selectedQuery, page.SearchText);
        Assert.Equal(selectedQuery, Assert.Single(notifiedQueries));
        Assert.Equal(page.ConvertQuery(selectedQuery).FormattedResult, page.GetItems()[0].Title);
    }
}