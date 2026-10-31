using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class FuelEconomyTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("6.5 L/100km to mpg US", 36.186858974359)]
    [InlineData("6.5 L/100 km to mpg UK", 43.458605589511)]
    [InlineData("10 L/100km to km/L", 10)]
    [InlineData("20 km/L to L/100km", 5)]
    [InlineData("10 liters per 100 kilometers to kilometers per liter", 10)]
    [InlineData("10 litres per 100 kilometres to km/L", 10)]
    [InlineData("1 mpg US to mpg UK", 1.200949925505)]
    [InlineData("30 US mpg to mpg (US)", 30)]
    [InlineData("30 imperial mpg to mpg UK", 30)]
    public void ConvertsEfficiencyAndReciprocalConsumption(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        // Units.NET 5.75 fuel factors differ from exact gallon definitions by less than one part per million.
        Assert.InRange(result.Output.Value, expected * (1 - 1e-6), expected * (1 + 1e-6));
        Assert.Equal(UnitType.FuelEconomy, result.Output.Unit.Type);
        var parsed = ConversionQueryParser.TryParseQuery(query, CultureInfo.InvariantCulture)!;
        var roundTrip = _service.ConvertQuery($"{result.Output.Value.ToString("R", CultureInfo.InvariantCulture)} {result.Output.Unit.Unit} to {parsed.FromUnit}", CultureInfo.InvariantCulture);
        Assert.True(roundTrip.Success, roundTrip.ErrorMessage);
        Assert.Equal(parsed.Value, roundTrip.Output.Value, 8);
    }

    [Theory]
    [InlineData("0 L/100km to mpg US")]
    [InlineData("-1 km/L to L/100km")]
    [InlineData("0 mpg US")]
    [InlineData("-6 L/100km")]
    [InlineData("1e-320 L/100km to km/L")]
    [InlineData("1 mpg US 2 mpg UK to km/L")]
    [InlineData("1 L/100km to L")]
    [InlineData("1 km/L² to L/100km")]
    public void RejectsInvalidValuesAndOperations(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void UnqualifiedMpgRequiresAnExplicitGallon()
    {
        var source = _service.ConvertQuery("30 mpg to L/100km", CultureInfo.InvariantCulture);
        Assert.False(source.Success);
        Assert.Contains("mpg US", source.ErrorMessage);
        Assert.Contains("mpg UK", source.ErrorMessage);
        foreach (var query in new[] { "30 mpg", "6.5 L/100km to mpg" })
        {
            var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
            Assert.False(result.Success);
            Assert.Contains(result.Completions, item => item.Unit.Unit == "mpg US");
            Assert.Contains(result.Completions, item => item.Unit.Unit == "mpg UK");
        }
    }

    [Fact]
    public void FrenchQueriesAndSuggestionsUseLocalizedFuelUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("6,5 litres aux 100 kilomètres en milles par gallon américain", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(36.186858974359, result.Output.Value, 8);
        Assert.Equal("mille par gallon américain", result.Output.Unit.Metadata!.DisplayName);
        var implicitResult = _service.ConvertQuery("6,5 L/100km", french);
        Assert.True(implicitResult.Success, implicitResult.ErrorMessage);
        Assert.Equal(3, implicitResult.Suggestions.Length);
        Assert.All(implicitResult.Suggestions, item => Assert.Equal(UnitType.FuelEconomy, item.Output.Unit.Type));
    }
}