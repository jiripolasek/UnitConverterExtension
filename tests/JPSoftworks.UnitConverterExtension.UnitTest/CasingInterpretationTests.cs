using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class CasingInterpretationTests
{
    private readonly ConversionService _service = new();
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    [Theory]
    [InlineData("10 LY", "10 ly")]
    [InlineData("12 IN", "12 in")]
    [InlineData("12 IN to CM", "12 in to cm")]
    [InlineData("12 IN IN CM", "12 in to cm")]
    [InlineData("10 Ly", "10 ly")]
    [InlineData("10 LY to KM", "10 ly to km")]
    [InlineData("10 LY in KM", "10 ly to km")]
    [InlineData("180 CM in FT IN", "180 cm to ft in")]
    [InlineData("(2 + 3) LY to km", "5 ly to km")]
    [InlineData("1 LY 2 LY to km", "3 ly to km")]
    [InlineData("1 KM² to FT²", "1 km² to ft²")]
    [InlineData("180 CM to FT IN", "180 cm to ft in")]
    [InlineData("5'11\" to CM", "5'11\" to cm")]
    public void UnambiguousCasingVariantsUseTheCanonicalConversion(string query, string canonical)
    {
        var expected = _service.ConvertQuery(canonical, English);
        var actual = _service.ConvertQuery(query, English);
        Assert.True(expected.Success, expected.ErrorMessage);
        Assert.True(actual.Success, actual.ErrorMessage);
        Assert.Equal(expected.Output.Value, actual.Output.Value, 8);
        Assert.Equal(expected.Output.Unit.Unit, actual.Output.Unit.Unit);
        Assert.Equal(expected.FormattedResult, actual.FormattedResult);
    }

    [Theory]
    [InlineData("10 mN to N", 0.01, "10 MN to N", 10000000)]
    [InlineData("10 mN in N", 0.01, "10 MN in N", 10000000)]
    [InlineData("10 MN to N", 10000000, "10 mN to N", 0.01)]
    [InlineData("1 mm to m", 0.001, "1 Mm to m", 1000000)]
    [InlineData("1 Mm to m", 1000000, "1 mm to m", 0.001)]
    [InlineData("1 MB to B", 1000000, "1 Mb to B", 125000)]
    public void ExactSymbolsStayPrimaryAndOfferTheOtherScale(string query, double value, string alternative, double otherValue)
    {
        var result = _service.ConvertQuery(query, English);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(value, result.Output.Value, 8);
        var interpretation = Assert.Single(result.OtherInterpretations, item => item.Query == alternative);
        var converted = _service.ConvertQuery(interpretation.Query, English);
        Assert.True(converted.Success, converted.ErrorMessage);
        Assert.Equal(otherValue, converted.Output.Value, 8);
        Assert.Equal(converted.FormattedResult, interpretation.FormattedResult);
        Assert.InRange(result.OtherInterpretations.Length, 1, 3);
    }

    [Theory]
    [InlineData("10 mn")]
    [InlineData("10 mn to N")]
    [InlineData("1 MM to m")]
    public void AmbiguousCasingRequiresAChoice(string query)
    {
        var result = _service.ConvertQuery(query, English);
        Assert.False(result.Success);
        Assert.True(result.IsIncomplete);
        Assert.InRange(result.OtherInterpretations.Length, 2, 3);
        Assert.Equal(result.OtherInterpretations.Length, result.OtherInterpretations.Select(item => item.Query).Distinct().Count());
        Assert.All(result.OtherInterpretations, item => Assert.True(_service.ConvertQuery(item.Query, English).Success));
    }

    [Fact]
    public void ExactSymbolsAndAliasesTakePriorityAcrossCategories()
    {
        foreach (var (query, type, unit, name) in new[]
        {
            ("1 W", UnitType.Power, "W", "Watt"),
            ("1 w", UnitType.Time, "w", "Week"),
            ("1 uA", UnitType.Current, "µA", "Microampere"),
            ("1 ua", UnitType.Length, "au", "Astronomical Unit")
        })
        {
            var result = _service.ConvertQuery(query, English);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(type, result.Output.Unit.Type);
            Assert.Equal(unit, result.Output.Unit.Unit);
            Assert.Equal(name, result.Output.Unit.Metadata!.DisplayName);
            Assert.Equal(result.Output.Unit.Metadata.Symbol, result.FormattedResult[2..]);
        }
    }

    [Fact]
    public void ImplicitExactInputKeepsItsUnitAndPreviewsTheAlternativeConversion()
    {
        var result = _service.ConvertQuery("10 mN", English);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("mN", result.Output.Unit.Unit);
        Assert.Equal(10, result.Output.Value);
        var alternative = Assert.Single(result.OtherInterpretations, item => item.Query == "10 MN");
        var converted = _service.ConvertQuery(alternative.Query, English);
        Assert.Equal(converted.Suggestions[0].FormattedResult, alternative.FormattedResult);
    }

    [Fact]
    public void AlternativesPreserveExpressionsAndTheConversionConnector()
    {
        var result = _service.ConvertQuery(" (2 + 3) mN as N ", English);
        var alternative = Assert.Single(result.OtherInterpretations, item => item.Query == "(2 + 3) MN as N");
        Assert.Equal("5000000 N", alternative.FormattedResult);
        Assert.Equal(0.005, result.Output.Value, 8);
    }

    [Fact]
    public void SourceAndTargetAlternativesRetainTheOtherUnit()
    {
        var result = _service.ConvertQuery("10 mN to MN", English);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1e-8, result.Output.Value, 12);
        Assert.Contains(result.OtherInterpretations, item => item.Query == "10 MN to MN");
        Assert.Contains(result.OtherInterpretations, item => item.Query == "10 mN to mN");
    }

    [Fact]
    public void PartialTargetsAcceptUnambiguousSourceCasing()
    {
        var result = _service.ConvertQuery("10 LY to", English);
        Assert.True(result.IsIncomplete);
        Assert.NotEmpty(result.Completions);
        Assert.All(result.Completions, item => Assert.True(_service.ConvertQuery(item.Query, English).Success));
    }

    [Theory]
    [InlineData("to")]
    [InlineData("in")]
    [InlineData("en")]
    public void PartialTargetsKeepAmbiguousSourceChoices(string connector)
    {
        var culture = connector == "en" ? CultureInfo.GetCultureInfo("fr-FR") : English;
        var result = _service.ConvertQuery($"10 mn {connector}", culture);
        Assert.True(result.IsIncomplete);
        Assert.False(result.Success);
        Assert.Contains(result.OtherInterpretations, item => item.Query == $"10 mN {connector} ");
        Assert.Contains(result.OtherInterpretations, item => item.Query == $"10 MN {connector} ");
    }

    [Fact]
    public void AliasesOfTheSameUnitDoNotCreateAlternativeInterpretations()
    {
        foreach (var query in new[] { "10 LY", "10 lightyears", "10 LIGHT YEARS", "10 newtons to N" })
        {
            var result = _service.ConvertQuery(query, English);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Empty(result.OtherInterpretations);
        }
    }

    [Theory]
    [InlineData(VolumeSystemPreference.US, 3.785411784)]
    [InlineData(VolumeSystemPreference.Imperial, 4.54609)]
    public void VolumePreferencesStillResolveUnqualifiedGallons(VolumeSystemPreference preference, double liters)
    {
        var service = new ConversionService(new DefaultUnitPreferenceProvider(preference));
        foreach (var query in new[] { "1 gal to l", "1 GAL to L" })
        {
            var result = service.ConvertQuery(query, English);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(liters, result.Output.Value, 8);
        }
    }

    [Fact]
    public void DeclaredAmbiguityAndInvalidValuesRemainFailures()
    {
        foreach (var query in new[] { "1 calorie to J", "1 / 0 LY", "1 LY to kg", "1e308 LY to km" })
        {
            Assert.False(_service.ConvertQuery(query, English).Success, query);
        }
    }

    [Theory]
    [InlineData("1 kN to km/h", "1 kn to km/h")]
    [InlineData("1 kn to N", "1 kN to N")]
    [InlineData("1e308 MN to N", "1e308 mN to N")]
    public void InvalidExactMeaningsRequireAnExplicitChoice(string query, string alternative)
    {
        var result = _service.ConvertQuery(query, English);
        Assert.False(result.Success);
        Assert.False(result.IsIncomplete);
        Assert.NotEmpty(result.ErrorMessage);
        Assert.Contains(result.OtherInterpretations, item => item.Query == alternative);
    }

    [Fact]
    public void CasingResolutionSupportsConcurrentCultures()
    {
        Parallel.For(0, 32, index =>
        {
            var culture = CultureInfo.GetCultureInfo(index % 2 == 0 ? "en-US" : "fr-FR");
            var result = _service.ConvertQuery("10 LY->km", culture);
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(UnitType.Length, result.Output.Unit.Type);
        });
    }
}