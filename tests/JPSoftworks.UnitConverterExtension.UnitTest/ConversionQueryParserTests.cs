// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class ConversionQueryParserTests
{
    private readonly ConversionQueryParser _parser;

    public ConversionQueryParserTests()
    {
        this._parser = new ConversionQueryParser();
    }

    [Theory]
    [InlineData("1 m to ft", 1.0, "m", "ft", false, UnitDimension.Linear)]
    [InlineData("100 °C to °F", 100.0, "°C", "°F", false, UnitDimension.Linear)]
    [InlineData("60 mins to hours", 60.0, "mins", "hours", false, UnitDimension.Linear)]
    [InlineData("1.5 kg to lb", 1.5, "kg", "lb", false, UnitDimension.Linear)]
    [InlineData("10 cm² to m²", 10.0, "cm", "m", false, UnitDimension.Squared)]
    [InlineData("1000 ml to l", 1000.0, "ml", "l", false, UnitDimension.Linear)]
    [InlineData("5.5 ft -> in", 5.5, "ft", "in", false, UnitDimension.Linear)]
    [InlineData("25 mph as kph", 25.0, "mph", "kph", false, UnitDimension.Linear)]
    public void TryParseQuery_ExplicitConversions_ParsesCorrectly(
        string query,
        double expectedValue,
        string expectedFromUnit,
        string expectedToUnit,
        bool expectedIsImplicit,
        UnitDimension expectedDimension)
    {
        // Act
        var result = this._parser.TryParseQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value, 2);
        Assert.Equal(expectedFromUnit, result.FromUnit);
        Assert.Equal(expectedToUnit, result.ToUnit);
        Assert.Equal(expectedIsImplicit, result.IsImplicitConversion);
        Assert.Equal(expectedDimension, result.Dimension);
        Assert.Equal(query, result.OriginalQuery);
    }

    [Theory]
    [InlineData("60 mins", 60.0, "mins", "", true, UnitDimension.Linear)]
    [InlineData("100 cm", 100.0, "cm", "", true, UnitDimension.Linear)]
    [InlineData("25 °C", 25.0, "°C", "", true, UnitDimension.Linear)]
    [InlineData("2.5 kg", 2.5, "kg", "", true, UnitDimension.Linear)]
    [InlineData("50 m²", 50.0, "m", "", true, UnitDimension.Squared)]
    [InlineData("1000 cm³", 1000.0, "cm", "", true, UnitDimension.Cubic)]
    [InlineData("15 square meters", 15.0, "meters", "", true, UnitDimension.Squared)]
    [InlineData("5 cubic feet", 5.0, "feet", "", true, UnitDimension.Cubic)]
    public void TryParseQuery_ImplicitConversions_ParsesCorrectly(
        string query,
        double expectedValue,
        string expectedFromUnit,
        string expectedToUnit,
        bool expectedIsImplicit,
        UnitDimension expectedDimension)
    {
        // Act
        var result = this._parser.TryParseQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedValue, result.Value, 2);
        Assert.Equal(expectedFromUnit, result.FromUnit);
        Assert.Equal(expectedToUnit, result.ToUnit);
        Assert.Equal(expectedIsImplicit, result.IsImplicitConversion);
        Assert.Equal(expectedDimension, result.Dimension);
        Assert.Equal(query, result.OriginalQuery);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid query")]
    [InlineData("to meters")]
    [InlineData("1 to")]
    [InlineData("meters to")]
    [InlineData("abc def")]
    public void TryParseQuery_InvalidQueries_ReturnsNull(string query)
    {
        // Act
        var result = this._parser.TryParseQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("1,5 m to ft", 1.5)] // European decimal separator
    [InlineData("1.5 m to ft", 1.5)] // US decimal separator
    [InlineData("-5 °C to °F", -5.0)] // Negative values
    [InlineData("+10 kg to lb", 10.0)] // Positive sign
    [InlineData("1 000 m to km", 1000.0)] // Space as thousands separator (some cultures)
    public void TryParseQuery_DifferentNumberFormats_ParsesCorrectly(string query, double expectedValue)
    {
        // Test with different cultures
        var cultures = new[]
        {
            CultureInfo.InvariantCulture, new CultureInfo("en-US"), new CultureInfo("de-DE"),
            new CultureInfo("fr-FR")
        };

        foreach (var culture in cultures)
        {
            // Act
            var result = this._parser.TryParseQuery(query, culture);

            // Assert - at least one culture should parse it correctly
            if (result != null)
            {
                Assert.Equal(expectedValue, result.Value, 2);
                return; // Success with at least one culture
            }
        }

        // If no culture could parse it, that's unexpected for these test cases
        Assert.True(false, $"No culture could parse: {query}");
    }

    [Theory]
    [InlineData("1 m² to cm²", UnitDimension.Squared)]
    [InlineData("1 sq ft to sq m", UnitDimension.Squared)]
    [InlineData("1 square meter to square feet", UnitDimension.Squared)]
    [InlineData("1 m³ to l", UnitDimension.Cubic)]
    [InlineData("1 cu ft to cu m", UnitDimension.Cubic)]
    [InlineData("1 cubic meter to cubic feet", UnitDimension.Cubic)]
    [InlineData("1 cm2 to m2", UnitDimension.Squared)]
    [InlineData("1 cm3 to m3", UnitDimension.Cubic)]
    public void TryParseQuery_DimensionalUnits_ParsesDimensionCorrectly(string query, UnitDimension expectedDimension)
    {
        // Act
        var result = this._parser.TryParseQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDimension, result.Dimension);
    }

    [Fact]
    public void TryParseQuery_ProblematicUnitsWithToSubstring_ParsesCorrectly()
    {
        // Test units that contain "to" substring but should not be split
        var testCases = new[]
        {
            ("10 stone", "stone"),
            ("5 stones", "stones"), 
            ("1 newton", "newton"),
            ("100 protons", "protons"),
            ("1 photon", "photon"),
            ("5 boston", "boston"), // hypothetical unit
            ("10 cotton", "cotton")  // hypothetical unit
        };

        foreach (var (query, expectedUnit) in testCases)
        {
            // Act
            var result = _parser.TryParseQuery(query, CultureInfo.InvariantCulture);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUnit, result.FromUnit);
            Assert.True(result.IsImplicitConversion);
        }
    }
}