using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class MixedQuantityTests
{
    [Theory]
    [InlineData("1 ft 2 in to cm", 35.56)]
    [InlineData("1 h 30 min to s", 5400)]
    [InlineData("1 kg 250 g to g", 1250)]
    [InlineData("1 l 250 ml to l", 1.25)]
    [InlineData("1 m2 100 cm2 to m2", 1.01)]
    [InlineData("-1 ft -2 in to cm", -35.56)]
    [InlineData("1 ft + 2 in to cm", 35.56)]
    [InlineData("1 ft 1 1/2 in to cm", 34.29)]
    [InlineData("1 ft (1 + 2) in to cm", 38.1)]
    [InlineData("1 m 1 000 mm to m", 2)]
    public void CompatibleComponentsAreAdded(string query, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("1 ft 2 kg to cm")]
    [InlineData("1 m2 2 cm3 to m2")]
    [InlineData("1 C 2 F to K")]
    [InlineData("1 m 2 cm to m2")]
    public void IncompatibleComponentsAreRejected(string query)
    {
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void MixedInputWithoutTargetOffersConversions()
    {
        var result = new ConversionService().ConvertQuery("1 h 30 min", CultureInfo.InvariantCulture);
        Assert.True(result.Success);
        Assert.Equal(1.5, result.Output.Value);
        Assert.Contains(result.Suggestions, suggestion => suggestion.Output.Unit.Unit == "min" && suggestion.Output.Value == 90);
    }
}