using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class ForceTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 lbf to N", 4.4482216152605)]
    [InlineData("1 kgf to N", 9.80665)]
    [InlineData("1 kN to N", 1000)]
    [InlineData("1 MN to N", 1000000)]
    [InlineData("1 mN to N", 0.001)]
    [InlineData("1 µN to N", 0.000001)]
    [InlineData("1 uN to mN", 0.001)]
    [InlineData("1 dyn to N", 0.00001)]
    [InlineData("2 POUNDS FORCE to N", 8.896443230521)]
    [InlineData("0 N to lbf", 0)]
    [InlineData("-2 kN to N", -2000)]
    [InlineData("1 kN 500 N to N", 1500)]
    [InlineData("(2 + 3) / 2 kN to N", 2500)]
    public void ConvertsForceUnitsAndExpressions(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(UnitType.Force, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 kg to N")]
    [InlineData("1 lb to lbf")]
    [InlineData("1 kn to N")]
    [InlineData("1 kN to km/h")]
    [InlineData("1 N² to kN²")]
    [InlineData("1e308 MN to N")]
    public void RejectsMassSpeedDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void KilonewtonAndKnotStayDistinctInSuggestionsAndTargets()
    {
        var force = _service.ConvertQuery("1 kN", CultureInfo.InvariantCulture);
        Assert.True(force.Success, force.ErrorMessage);
        Assert.Equal(UnitType.Force, force.Output.Unit.Type);
        Assert.All(force.Suggestions, item => Assert.Equal(UnitType.Force, item.Output.Unit.Type));
        var speed = _service.ConvertQuery("1 kn to km/h", CultureInfo.InvariantCulture);
        Assert.True(speed.Success, speed.ErrorMessage);
        Assert.Equal(1.852, speed.Output.Value, 9);
        Assert.Equal(UnitType.Speed, speed.Output.Unit.Type);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseForceUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 kilonewtons en newtons", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1500, result.Output.Value, 9);
        Assert.Equal("newton", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 kilogramme-force", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "N" && Math.Abs(item.Output.Value - 9.80665) < 1e-9);
        var incomplete = _service.ConvertQuery("1 lbf en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Force, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}