using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class TorqueTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 lbf·ft to N·m", 1.3558179483314)]
    [InlineData("1 lbf·in to Nm", 0.1129848290276167)]
    [InlineData("1 kgf·m to Nm", 9.80665)]
    [InlineData("1 kNm to Nm", 1000)]
    [InlineData("100 N·cm to Nm", 1)]
    [InlineData("1000 N·mm to Nm", 1)]
    [InlineData("1 N*m to Ncm", 100)]
    [InlineData("1 N m to Nmm", 1000)]
    [InlineData("1 lbf ft to lbf in", 12)]
    [InlineData("2 NEWTON METRES to Ncm", 200)]
    [InlineData("0 Nm to lbf·ft", 0)]
    [InlineData("-2 Nm to Ncm", -200)]
    [InlineData("1 Nm 50 Ncm to Nm", 1.5)]
    [InlineData("2^3 Nm to Ncm", 800)]
    public void ConvertsTorqueAndKeyboardSpellings(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(UnitType.Torque, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 Nm to J")]
    [InlineData("1 Nm to N")]
    [InlineData("1 nm to Nm")]
    [InlineData("1 Nm² to Ncm²")]
    [InlineData("1e308 kNm to Nm")]
    public void RejectsOtherQuantitiesDimensionsAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Theory]
    [InlineData("1 nm to pm", 1000, UnitType.Length)]
    [InlineData("1 ft·lb to J", 1.3558179483314, UnitType.Energy)]
    [InlineData("1 lbf·ft to J", 1.3558179483314, UnitType.Energy)]
    public void PreservesNanometersAndExistingFootPoundEnergy(string query, double expected, UnitType type)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 9);
        Assert.Equal(type, result.Output.Unit.Type);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseTorqueUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 newtons-mètres en newtons-centimètres", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(150, result.Output.Value, 9);
        Assert.Equal("newton-centimètre", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 lbf·ft", french);
        Assert.Equal(UnitType.Torque, implicitResult.Output.Unit.Type);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "Nm");
        var incomplete = _service.ConvertQuery("1 Nm en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Torque, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}