using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class DensityTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 g/cm³ to kg/m³", 1000)]
    [InlineData("1 g/cm3 to kg/m^3", 1000)]
    [InlineData("1000 kg/m3 to g/cm^3", 1)]
    [InlineData("1 g/mL to kg/m³", 1000)]
    [InlineData("1 kg/L to g/mL", 1)]
    [InlineData("1 lb/ft³ to kg/m³", 16.01846337396014)]
    [InlineData("1 lb/in³ to kg/m³", 27679.90471020312)]
    [InlineData("1 lb/in3 to lb/ft^3", 1728)]
    [InlineData("1728 lb/ft3 to lb/in3", 1)]
    [InlineData("16.01846337396014 kg/m3 to lb/ft3", 1)]
    [InlineData("27679.90471020312 kg/m3 to lb/in3", 1)]
    [InlineData("2 GRAMS PER CUBIC CENTIMETRE to kg/m³", 2000)]
    [InlineData("0 g/cm³ to kg/m³", 0)]
    [InlineData("-1 kg/L to kg/m³", -1000)]
    [InlineData("1 kg/L 500 kg/m³ to kg/m³", 1500)]
    [InlineData("(2 + 3) / 2 g/cm3 to kg/m3", 2500)]
    public void ConvertsDensityWithoutCubingTheWholeConversionFactor(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
        Assert.Equal(UnitType.Density, result.Output.Unit.Type);
        Assert.Equal(UnitDimension.Linear, result.Output.Unit.Dimension);
    }

    [Theory]
    [InlineData("1 kg/m³ to kg")]
    [InlineData("1 kg/m³ to m³")]
    [InlineData("1 kg/m² to lb/ft²")]
    [InlineData("1 kg/m³³ to lb/ft³³")]
    [InlineData("1e308 g/cm³ to kg/m³")]
    public void RejectsMassVolumeInvalidPowersAndOverflow(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void CubicLengthsRemainVolumeConversions()
    {
        var result = _service.ConvertQuery("1 m3 to L", CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1000, result.Output.Value, 8);
        Assert.Equal(UnitType.Volume, result.Output.Unit.Type);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseDensityUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 grammes par centimètre cube en kilogrammes par mètre cube", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1500, result.Output.Value, 8);
        Assert.Equal("kilogramme par mètre cube", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("1 kilogramme par litre", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "kg/m³" && item.Output.Value == 1000);
        var incomplete = _service.ConvertQuery("1 g/cm3 en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.Density, item.Unit.Type));
        Assert.All(incomplete.Completions, item => Assert.True(_service.ConvertQuery(item.Query, french).Success));
    }
}