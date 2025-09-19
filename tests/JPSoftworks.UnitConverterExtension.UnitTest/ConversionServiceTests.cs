// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class ConversionServiceTests
{
    private readonly ConversionService _service;

    public ConversionServiceTests()
    {
        this._service = new ConversionService();
    }

    [Theory]
    [InlineData("1 m to ft", true, 1.0, "m", "ft")]
    [InlineData("100 cm to meters", true, 100.0, "cm", "meters")]
    [InlineData("1 kg to lb", true, 1.0, "kg", "lb")]
    [InlineData("1 t to kg", true, 1.0, "t", "kg")]
    [InlineData("16 oz to lb", true, 16.0, "oz", "lb")]
    [InlineData("1 stone to lb", true, 1.0, "stone", "lb")]    // Stone unit test
    [InlineData("2 stones to kg", true, 2.0, "stones", "kg")] // Plural stone test
    [InlineData("5 metric tons to kg", true, 5.0, "metric tons", "kg")] // Multi-word unit
    [InlineData("10 short tons to lb", true, 10.0, "short tons", "lb")] // Multi-word unit
    [InlineData("1 pound to stone", true, 1.0, "pound", "stone")] // To stone conversion
    public void ConvertQuery_MassAndStoneConversions_ReturnsCorrectResults(
        string query,
        bool expectedSuccess,
        double expectedInputValue,
        string expectedFromUnit,
        string expectedToUnit)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            // Verify the conversion succeeded and parsing worked correctly
            Assert.True(result.Output.Value > 0, "Converted value should be positive");
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
            
            // Verify the conversion contains the expected to-unit in the formatted result
            Assert.Contains(expectedToUnit, result.FormattedResult, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData("0 °C to °F", true, 32.0, "°F")]
    [InlineData("100 °C to °F", true, 212.0, "°F")]
    [InlineData("32 °F to °C", true, 0.0, "°C")]
    [InlineData("212 °F to °C", true, 100.0, "°C")]
    [InlineData("-40 °C to °F", true, -40.0, "°F")] // Special case where C and F are equal
    [InlineData("273.15 K to °C", true, 0.0, "°C")]
    public void ConvertQuery_TemperatureConversions_ReturnsCorrectResults(
        string query,
        bool expectedSuccess,
        double expectedValue,
        string expectedUnit)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedValue, result.Output.Value,
                1); // Temperature conversions might have small rounding errors
            Assert.Equal(expectedUnit, result.Output.Unit.Unit);
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("60 min to h", true, 1.0, "h")]
    [InlineData("1 h to min", true, 60.0, "min")]
    [InlineData("3600 s to h", true, 1.0, "h")]
    [InlineData("1 d to h", true, 24.0, "h")]
    [InlineData("1 week to d", true, 7.0, "d")]
    public void ConvertQuery_TimeConversions_ReturnsCorrectResults(
        string query,
        bool expectedSuccess,
        double expectedValue,
        string expectedUnit)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedValue, result.Output.Value, 2);
            Assert.Equal(expectedUnit, result.Output.Unit.Unit);
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("1 kg to lb", true, 2.205, "lb")]
    [InlineData("1 lb to kg", true, 0.454, "kg")]
    [InlineData("1000 g to kg", true, 1.0, "kg")]
    [InlineData("1 t to kg", true, 1000.0, "kg")]
    [InlineData("16 oz to lb", true, 1.0, "lb")]
    public void ConvertQuery_MassConversions_ReturnsCorrectResults(
        string query,
        bool expectedSuccess,
        double expectedValue,
        string expectedUnit)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedValue, result.Output.Value, 3);
            Assert.Equal(expectedUnit, result.Output.Unit.Unit);
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("1 gal to l", true, 3.785, "l")] // US gallon
    [InlineData("1 l to ml", true, 1000.0, "ml")]
    [InlineData("1000 ml to l", true, 1.0, "l")]
    [InlineData("1 cup to ml", true, 236.588, "ml")] // US cup
    [InlineData("1 tsp to ml", true, 4.929, "ml")] // US teaspoon
    public void ConvertQuery_VolumeConversions_ReturnsCorrectResults(
        string query,
        bool expectedSuccess,
        double expectedValue,
        string expectedUnit)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedValue, result.Output.Value, 3);
            Assert.Equal(expectedUnit, result.Output.Unit.Unit);
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("60 mins", true, 60.0, "min")]
    [InlineData("100 cm", true, 100.0, "cm")]
    [InlineData("25 °C", true, 25.0, "°C")]
    [InlineData("2.5 kg", true, 2.5, "kg")]
    [InlineData("1000 ml", true, 1000.0, "ml")]
    public void ConvertQuery_ImplicitConversions_ReturnsSuggestions(
        string query,
        bool expectedSuccess,
        double expectedValue,
        string expectedUnit)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.Equal(expectedValue, result.Output.Value);
            Assert.Equal(expectedUnit, result.Output.Unit.Unit);
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
            Assert.NotEmpty(result.Suggestions);

            // All suggestions should have proper formatting
            Assert.All(result.Suggestions, suggestion =>
            {
                Assert.NotEmpty(suggestion.Output.Unit.Unit);
                Assert.True(suggestion.Output.Value > 0);
                Assert.NotEmpty(suggestion.FormattedResult);
            });
        }
    }

    [Theory]
    [InlineData("invalid query")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("1 to")]
    [InlineData("to meters")]
    [InlineData("1 unknown_unit to m")]
    [InlineData("1 m to unknown_unit")]
    [InlineData("1 m to kg")] // Different unit categories
    [InlineData("1 °C to m")] // Temperature to length
    public void ConvertQuery_InvalidQueries_ReturnsFailure(string query)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage);
        Assert.Equal(0, result.Output.Value);
        Assert.True(string.IsNullOrEmpty(result.Output.Unit.Unit));
        Assert.Empty(result.FormattedResult);
        Assert.Empty(result.Suggestions);
    }

    [Theory]
    [InlineData("1,5 m to ft")] // European decimal separator
    [InlineData("1.5 m to ft")] // US decimal separator
    [InlineData("-5 °C to °F")] // Negative values
    [InlineData("+10 kg to lb")] // Positive sign
    public void ConvertQuery_DifferentCultures_HandlesCorrectly(string query)
    {
        var cultures = new[]
        {
            CultureInfo.InvariantCulture, new CultureInfo("en-US"), new CultureInfo("de-DE"),
            new CultureInfo("fr-FR")
        };

        var successCount = 0;

        foreach (var culture in cultures)
        {
            // Act
            var result = this._service.ConvertQuery(query, culture);

            // Assert - at least one culture should handle it
            if (result.Success)
            {
                successCount++;
                Assert.True(result.Output.Value != 0 || query.Contains("0"));
                Assert.NotEmpty(result.FormattedResult);
            }
        }

        // At least one culture should successfully parse these common patterns
        Assert.True(successCount > 0, $"No culture could handle query: {query}");
    }

    [Fact]
    public void GetAllSupportedUnits_ReturnsAllCategories()
    {
        // Act
        var supportedUnits = this._service.GetAllSupportedUnits();

        // Assert
        Assert.NotEmpty(supportedUnits);

        // Check expected categories
        var expectedCategories = new[] { "Length", "Temperature", "Time", "Volume", "Mass" };
        foreach (var category in expectedCategories)
        {
            Assert.True(supportedUnits.ContainsKey(category), $"Missing category: {category}");
            Assert.NotEmpty(supportedUnits[category]);
        }
    }

    [Theory]
    [InlineData("1 m² to cm²", true)] // Area conversion
    [InlineData("1 ft² to m²", true)] // Area conversion
    [InlineData("1 m³ to l", true)] // Volume conversion (cubic meters to liters)
    [InlineData("1 ft³ to m³", true)] // Volume conversion
    public void ConvertQuery_DimensionalUnits_HandlesCorrectly(string query, bool expectedSuccess)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedSuccess)
        {
            Assert.True(result.Output.Value > 0);
            Assert.NotEmpty(result.FormattedResult);
            Assert.Empty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData(0.0, "m", "ft")]
    [InlineData(-10.0, "°C", "°F")]
    [InlineData(1000000.0, "mm", "km")]
    public void ConvertQuery_EdgeValues_HandlesCorrectly(double value, string fromUnit, string toUnit)
    {
        // Arrange
        var query = $"{value} {fromUnit} to {toUnit}";

        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        // Should either succeed or fail gracefully
        if (result.Success)
        {
            Assert.False(double.IsNaN(result.Output.Value));
            Assert.False(double.IsInfinity(result.Output.Value));
            Assert.NotEmpty(result.FormattedResult);
        }
        else
        {
            Assert.NotEmpty(result.ErrorMessage);
        }
    }
}