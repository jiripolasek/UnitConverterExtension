using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class AccelerationTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 g0 to m/s²", 9.80665)]
    [InlineData("1 g₀ to m/s2", 9.80665)]
    [InlineData("1 ft/s² to m/s²", 0.3048)]
    [InlineData("1 in/s² to m/s²", 0.0254)]
    [InlineData("1 m/s² to cm/s²", 100)]
    [InlineData("1 m/s^2 to mm/s2", 1000)]
    [InlineData("1 ft/s2 to in/s^2", 12)]
    [InlineData("1 METRE PER SECOND SQUARED to cm/s²", 100)]
    [InlineData("0 g0 to m/s²", 0)]
    [InlineData("-1 g0 to m/s²", -9.80665)]
    [InlineData("1 m/s² 50 cm/s² to m/s²", 1.5)]
    [InlineData("(2 + 3) / 2 m/s^2 to cm/s2", 250)]
    public void ConvertsAccelerationWithoutSquaringTheConversionFactor(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(UnitType.Acceleration, result.Output.Unit.Type);
        Assert.Equal(UnitDimension.Linear, result.Output.Unit.Dimension);
    }

    [Theory]
    [InlineData("1 g to m/s²")]
    [InlineData("1 m/s² to m/s")]
    [InlineData("1 m/s² to m²")]
    [InlineData("1 m/s³ to ft/s³")]
    [InlineData("1 m/s²² to ft/s²²")]
    [InlineData("1e308 g0 to mm/s²")]
    public void RejectsMassSpeedInvalidPowersAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void GramsAndSquaredLengthsKeepTheirOriginalMeaning()
    {
        var mass = _service.ConvertQuery("1 g to mg", CultureInfo.InvariantCulture);
        Assert.True(mass.Success, mass.ErrorMessage);
        Assert.Equal(1000, mass.Output.Value, 9);
        Assert.Equal(UnitType.Mass, mass.Output.Unit.Type);
        var area = _service.ConvertQuery("1 m2 to cm2", CultureInfo.InvariantCulture);
        Assert.True(area.Success, area.ErrorMessage);
        Assert.Equal(10000, area.Output.Value, 9);
        Assert.Equal(UnitType.Area, area.Output.Unit.Type);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseAccelerationUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 mètres par seconde carrée en centimètres par seconde carrée", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(150, result.Output.Value, 9);
        Assert.Equal("centimètre par seconde carrée", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 gravité standard", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "m/s²" && Math.Abs(item.Output.Value - 9.80665) < 1e-9);
        var incomplete = _service.ConvertQuery("1 m/s2 en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Acceleration, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}