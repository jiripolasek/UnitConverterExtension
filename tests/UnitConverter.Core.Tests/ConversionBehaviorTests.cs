using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class ConversionBehaviorTests
{
    [Theory]
    [InlineData("1 m2 to cm3")]
    [InlineData("1 m3 to cm2")]
    [InlineData("1 square celsius to square fahrenheit")]
    [InlineData("1 cubic kilogram to cubic pound")]
    [InlineData("1 square celsius")]
    public void UnsupportedDimensionsAreRejected(string query)
    {
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Theory]
    [InlineData(AccuracyLevel.CommonUse)]
    [InlineData(AccuracyLevel.Enhanced)]
    [InlineData(AccuracyLevel.ExtremelyPrecise)]
    public void SmallNonzeroResultsRemainNonzero(AccuracyLevel accuracy)
    {
        var service = new ConversionService(null, new DefaultAccuracyPreferenceProvider(accuracy));
        foreach (var cultureName in new[] { "en-US", "fr-FR" })
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var result = service.ConvertQuery("1 fm->m", culture);
            Assert.True(result.Success);
            var displayed = double.Parse(result.FormattedResult.Split(' ')[0], NumberStyles.Float, culture);
            Assert.Equal(result.Output.Value, displayed);
            Assert.NotEqual(0, displayed);
        }
    }

    [Theory]
    [InlineData(AccuracyLevel.CommonUse)]
    [InlineData(AccuracyLevel.Enhanced)]
    [InlineData(AccuracyLevel.ExtremelyPrecise)]
    public void SuggestionsUseTheSameAccuracyAndCultureAsExplicitResults(AccuracyLevel accuracy)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            var culture = CultureInfo.GetCultureInfo("en-US");
            var service = new ConversionService(null, new DefaultAccuracyPreferenceProvider(accuracy));
            var implicitResult = service.ConvertQuery("1 m", culture);
            var explicitResult = service.ConvertQuery("1 m to ft", culture);
            var feet = Assert.Single(implicitResult.Suggestions, suggestion => suggestion.Output.Unit.Unit == "ft");
            Assert.Equal(explicitResult.FormattedResult, feet.FormattedResult);
            Assert.DoesNotContain(',', feet.FormattedResult);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}