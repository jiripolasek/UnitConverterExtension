using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class QueryGuidanceTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 m to", "en-US", "1 m to cm")]
    [InlineData("1 m in", "en-US", "1 m in cm")]
    [InlineData("1 m in f", "en-US", "1 m in ft")]
    [InlineData("1 ft 2 in in c", "en-US", "1 ft 2 in in cm")]
    [InlineData("1 in in c", "en-US", "1 in in cm")]
    [InlineData("1 lbf in in N", "en-US", "1 lbf in in Nm")]
    [InlineData("1,5 m in centim", "fr-FR", "1,5 m in cm")]
    [InlineData("1 m => c", "en-US", "1 m => cm")]
    [InlineData("1 m en centim", "fr-FR", "1 m en cm")]
    [InlineData("1 kilogr", "en-US", "1 kg")]
    [InlineData("1 centimè", "fr-FR", "1 cm")]
    [InlineData("1 ft 2 in to c", "en-US", "1 ft 2 in to cm")]
    public void IncompleteQueriesOfferUsableCompletions(string query, string cultureName, string expected)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var result = _service.ConvertQuery(query, culture, ConversionWordLanguage.InputLanguageAndEnglish);
        Assert.False(result.Success);
        Assert.True(result.IsIncomplete);
        Assert.Contains(result.Completions, completion => completion.Query == expected);
        Assert.All(result.Completions, completion => Assert.True(_service.ConvertQuery(completion.Query, culture, ConversionWordLanguage.InputLanguageAndEnglish).Success));
    }

    [Theory]
    [InlineData("1 m to", UnitType.Length)]
    [InlineData("1 kg to", UnitType.Mass)]
    [InlineData("1 m² to", UnitType.Area)]
    [InlineData("1 l to", UnitType.Volume)]
    public void TargetsStayInTheSourceDimension(string query, UnitType expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.NotEmpty(result.Completions);
        Assert.All(result.Completions, completion => Assert.Equal(expected, completion.Unit.Type));
    }

    [Fact]
    public void IncompatibleTargetsOfferCorrectionsWithoutClaimingSuccess()
    {
        var result = _service.ConvertQuery("1 m to kg", CultureInfo.InvariantCulture);
        Assert.False(result.Success);
        Assert.False(result.IsIncomplete);
        Assert.NotEmpty(result.Completions);
        Assert.All(result.Completions, completion => Assert.Equal(UnitType.Length, completion.Unit.Type));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("(1 + 2) / 2")]
    public void AValueWithoutAUnitOffersSourceUnits(string query)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.IsIncomplete);
        Assert.NotEmpty(result.Completions);
        Assert.All(result.Completions, completion => Assert.Empty(completion.FormattedResult));
    }

    [Theory]
    [InlineData("1 +", true)]
    [InlineData("(2 + 3) *", true)]
    [InlineData("1 / 0 m", false)]
    [InlineData("words", false)]
    public void UnfinishedArithmeticDiffersFromInvalidInput(string query, bool incomplete)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.False(result.Success);
        Assert.Equal(incomplete, result.IsIncomplete);
    }

    [Theory]
    [InlineData("10 N to", "en-US")]
    [InlineData("1 m² to", "en-US")]
    [InlineData("1 m³ to", "en-US")]
    [InlineData("1 ft 2 in to", "en-US")]
    [InlineData("1,5 m en", "fr-FR")]
    [InlineData("0 °C to", "en-US")]
    [InlineData("12 km/h to", "en-US")]
    public void TargetCompletionsPreviewValuesAndOmitTheSourceUnit(string query, string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var result = _service.ConvertQuery(query, culture);
        Assert.True(result.IsIncomplete);
        Assert.NotEmpty(result.Completions);
        var sourceQuery = query[..query.LastIndexOf(' ')];
        var source = _service.ConvertQuery(sourceQuery, culture);
        Assert.True(source.Success, source.ErrorMessage);
        Assert.All(result.Completions, completion =>
        {
            var conversion = _service.ConvertQuery(completion.Query, culture);
            Assert.True(conversion.Success, conversion.ErrorMessage);
            Assert.Equal(conversion.FormattedResult, completion.FormattedResult);
            Assert.Equal(conversion.Output.Unit.Unit, completion.Unit.Unit);
            Assert.Equal(conversion.Output.Unit.Type, completion.Unit.Type);
            Assert.Equal(conversion.Output.Unit.Dimension, completion.Unit.Dimension);
            Assert.False(completion.Unit.Unit == source.Output.Unit.Unit && completion.Unit.Dimension == source.Output.Unit.Dimension);
        });
    }

    [Fact]
    public void AnExplicitSourceUnitPrefixCanStillCompleteToTheSameUnit()
    {
        var result = _service.ConvertQuery("10 N to newt", CultureInfo.InvariantCulture);
        var completion = Assert.Single(result.Completions, item => item.Query == "10 N to N");
        Assert.Equal("10 N", completion.FormattedResult);
    }
}