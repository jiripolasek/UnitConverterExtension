using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class PaceTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("5 min/km to km/h", 12)]
    [InlineData("12 km/h to min/km", 5)]
    [InlineData("6 mph to min/mi", 10)]
    [InlineData("10 min/mi to mph", 6)]
    [InlineData("5 min/km to min/mi", 8.04672)]
    [InlineData("10 min/mi to min/km", 6.21371192237334)]
    [InlineData("300 s/km to min/km", 5)]
    [InlineData("480 s/mi to min/mi", 8)]
    [InlineData("5:30 min/km to km/h", 10.9090909090909)]
    [InlineData("5:30min/mi to mph", 10.9090909090909)]
    [InlineData("5:30.5 min/km to s/km", 330.5)]
    [InlineData("5 min/km 30 s/km to s/km", 330)]
    [InlineData("6 km/h 6 km/h to min/km", 5)]
    [InlineData("5 minutes per kilometre to km/h", 12)]
    [InlineData("(2 + 3) min/mile to mph", 12)]
    public void ConvertsPaceToPaceAndSpeed(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
        Assert.Equal(UnitType.Speed, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("5.5 min/km to min/km", "5:30 min/km")]
    [InlineData("12 km/h to min/km", "5:00 min/km")]
    [InlineData("300 s/km to min/mi", "8:02.8 min/mi")]
    [InlineData("5.999999 min/km to min/km", "6:00 min/km")]
    [InlineData("0.1 min/km to min/km", "0:06 min/km")]
    [InlineData("1e-10 min/km to min/km", "1E-10 min/km")]
    public void MinutePaceFormatsAsMinutesAndSecondsWithRoundingCarry(string query, string expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.FormattedResult);
        Assert.True(_service.ConvertQuery(result.FormattedResult, CultureInfo.InvariantCulture).Success);
    }

    [Theory]
    [InlineData(AccuracyLevel.CommonUse, "6:00 min/km")]
    [InlineData(AccuracyLevel.Enhanced, "5:59.999 min/km")]
    [InlineData(AccuracyLevel.ExtremelyPrecise, "5:59.9994 min/km")]
    public void PaceSecondsRespectTheSelectedAccuracy(AccuracyLevel accuracy, string expected)
    {
        var service = new ConversionService(null, new DefaultAccuracyPreferenceProvider(accuracy));
        var result = service.ConvertQuery("5.99999 min/km to min/km", CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.FormattedResult);
    }

    [Theory]
    [InlineData("0 min/km")]
    [InlineData("-5 min/km to km/h")]
    [InlineData("0 km/h to min/km")]
    [InlineData("-12 km/h to min/km")]
    [InlineData("1e-320 min/km to m/s")]
    [InlineData("1e308 min/km to s/km")]
    [InlineData("5:60 min/km to km/h")]
    [InlineData("5:3 min/km to km/h")]
    [InlineData("5:30:00 min/km to km/h")]
    [InlineData("5:30 m to cm")]
    [InlineData("5:30 s/km to min/km")]
    [InlineData("-5:30 min/km to km/h")]
    [InlineData("5 min/km² to km/h")]
    [InlineData("5 min/km to km")]
    [InlineData("5 min/km 1 km/h to km/h")]
    [InlineData("1 km/h 5 min/km to min/km")]
    public void RejectsInvalidPacesAndIncompatibleArithmetic(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void OrdinaryZeroAndNegativeSpeedsStillConvertWithoutPaceSuggestions()
    {
        var zero = _service.ConvertQuery("0 km/h", CultureInfo.InvariantCulture);
        Assert.True(zero.Success, zero.ErrorMessage);
        Assert.DoesNotContain(zero.Suggestions, item => item.Output.Unit.Unit is "min/km" or "min/mi");
        var negative = _service.ConvertQuery("-1 m/s to km/h", CultureInfo.InvariantCulture);
        Assert.True(negative.Success, negative.ErrorMessage);
        Assert.Equal(-3.6, negative.Output.Value, 8);
    }

    [Fact]
    public void FrenchPacesAndGuidanceIncludeCompatibleSpeeds()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("5,5 minutes par kilomètre en secondes par kilomètre", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(330, result.Output.Value, 8);
        Assert.Equal("seconde par kilomètre", result.Output.Unit.Metadata!.DisplayName);
        var clock = _service.ConvertQuery("5:30,5 min/km en min/km", french);
        Assert.True(clock.Success, clock.ErrorMessage);
        Assert.Equal("5:30,5 min/km", clock.FormattedResult);
        var incomplete = _service.ConvertQuery("5:30 min/km en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.Contains(incomplete.Completions, item => item.Unit.Unit == "km/h");
        Assert.Contains(incomplete.Completions, item => item.Unit.Unit == "min/mi");
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
        var speed = _service.ConvertQuery("12 km/h", french);
        Assert.Contains(speed.Suggestions, item => item.Output.Unit.Unit == "min/km" && item.FormattedResult == "5:00 min/km");
    }
}