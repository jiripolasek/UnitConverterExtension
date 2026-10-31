using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class AdditionalConversionTests
{
    [Theory]
    [InlineData("1 m")]
    [InlineData("1 m to ft")]
    [InlineData("1 m² to ft²")]
    [InlineData("1 m³ to l")]
    [InlineData("180 cm to ft in")]
    [InlineData("100 Mbps to MB/s")]
    [InlineData("0 °C to °F")]
    [InlineData("10 delta C to delta F")]
    [InlineData("6.5 L/100km to mpg US")]
    [InlineData("12 km/h to min/km")]
    [InlineData("-1 kg to lb")]
    [InlineData("1 N·m to lbf·ft")]
    public void AdditionalConversionsPreserveTheQuantityAndExcludeDisplayedUnits(string query)
    {
        var culture = CultureInfo.GetCultureInfo("en-US");
        var service = new ConversionService();
        var result = service.ConvertQuery(query, culture);
        Assert.True(result.Success, result.ErrorMessage);
        var conversions = service.GetAdditionalConversions(result, culture).Take(5).ToArray();
        Assert.NotEmpty(conversions);
        Assert.Equal(conversions.Length, conversions.Select(item => item.Output.Unit.Unit).Distinct().Count());

        foreach (var conversion in conversions)
        {
            var output = conversion.Output;
            Assert.True(double.IsFinite(output.Value));
            Assert.Equal(result.Output.Unit.Type, output.Unit.Type);
            Assert.Equal(result.Output.Unit.Dimension, output.Unit.Dimension);
            Assert.NotEqual(result.Output.Unit.Unit, output.Unit.Unit);
            Assert.DoesNotContain(result.Suggestions, item => item.Output.Unit.Unit == output.Unit.Unit);
            var suffix = output.Unit.Dimension switch { UnitDimension.Squared => "²", UnitDimension.Cubic => "³", _ => "" };
            var roundTrip = service.ConvertQuery($"{output.Value.ToString("R", culture)} {output.Unit.Unit}{suffix} to {result.Output.Unit.Unit}{suffix}", culture);
            Assert.True(roundTrip.Success, roundTrip.ErrorMessage);
            Assert.Equal(result.Output.Value, roundTrip.Output.Value, 8);
        }
    }

    [Theory]
    [InlineData("0 m")]
    [InlineData("1e-20 m")]
    [InlineData("1e20 m")]
    public void AdditionalConversionsKeepFiniteZeroAndExtremeValues(string query)
    {
        var culture = CultureInfo.InvariantCulture;
        var service = new ConversionService();
        var conversions = service.GetAdditionalConversions(service.ConvertQuery(query, culture), culture).Take(5).ToArray();
        Assert.NotEmpty(conversions);
        Assert.All(conversions, conversion => Assert.True(double.IsFinite(conversion.Output.Value)));
    }

    [Fact]
    public void FailedConversionsHaveNoAdditionalConversions()
    {
        var service = new ConversionService();
        Assert.Empty(service.GetAdditionalConversions(service.ConvertQuery("1 kg to m"), CultureInfo.InvariantCulture));
    }
}