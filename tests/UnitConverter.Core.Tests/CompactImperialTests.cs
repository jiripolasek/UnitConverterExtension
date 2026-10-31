using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class CompactImperialTests
{
    [Theory]
    [InlineData("5'11\" to cm", 180.34)]
    [InlineData("5′11″ to cm", 180.34)]
    [InlineData("5’11” to cm", 180.34)]
    [InlineData("5ft11in to cm", 180.34)]
    [InlineData("5FT11IN to cm", 180.34)]
    [InlineData("5' 11.5\" to cm", 181.61)]
    [InlineData("5'11½\" to cm", 181.61)]
    [InlineData("5'11 1/2\" to cm", 181.61)]
    [InlineData("-5'11\" to cm", -180.34)]
    [InlineData("-5ft11in to cm", -180.34)]
    [InlineData("0'6\" to cm", 15.24)]
    public void CompactMeasurementsConvertCorrectly(string query, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("5'11 to cm")]
    [InlineData("5ft11 to cm")]
    [InlineData("5'-11\" to cm")]
    [InlineData("5'1/0\" to cm")]
    public void IncompleteOrInvalidCompactInputIsRejected(string query) =>
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture).Success);

    [Fact]
    public void CompactInputUsesTheSelectedNumberCulture()
    {
        var result = new ConversionService().ConvertQuery("5'11,5\" en cm", CultureInfo.GetCultureInfo("fr-FR"));
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(181.61, result.Output.Value, 8);
    }
}