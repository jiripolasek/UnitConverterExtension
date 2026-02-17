// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class UnitsNetLengthModuleTests
{
    private readonly UnitsNetLengthModule _module;

    public UnitsNetLengthModuleTests()
    {
        this._module = new UnitsNetLengthModule();
    }

    [Fact]
    public void Properties_ReturnsCorrectValues()
    {
        // Assert
        Assert.Equal("Length", this._module.Category);
        Assert.True(this._module.SupportsSquaredUnits);
        Assert.True(this._module.SupportsCubicUnits);
    }

    [Theory]
    [InlineData("m", "m")]
    [InlineData("meter", "m")]
    [InlineData("metre", "m")]
    [InlineData("meters", "m")]
    [InlineData("metres", "m")]
    [InlineData("cm", "cm")]
    [InlineData("centimeter", "cm")]
    [InlineData("centimetre", "cm")]
    [InlineData("ft", "ft")]
    [InlineData("foot", "ft")]
    [InlineData("feet", "ft")]
    [InlineData("'", "ft")]
    [InlineData("in", "in")]
    [InlineData("inch", "in")]
    [InlineData("inches", "in")]
    [InlineData("\"", "in")]
    [InlineData("km", "km")]
    [InlineData("kilometer", "km")]
    [InlineData("kilometre", "km")]
    [InlineData("mi", "mi")]
    [InlineData("mile", "mi")]
    [InlineData("miles", "mi")]
    [InlineData("yd", "yd")]
    [InlineData("yard", "yd")]
    [InlineData("yards", "yd")]
    [InlineData("nmi", "nmi")]
    [InlineData("nautical mile", "nmi")]
    [InlineData("nanometer", "nm")]
    public void TryNormalizeUnit_ValidUnits_ReturnsCorrectNormalizedUnit(string input, string expected)
    {
        // Act
        var success = this._module.TryNormalizeUnit(input, CultureInfo.InvariantCulture, out var normalized);

        // Assert
        Assert.True(success);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("°C")]
    [InlineData("seconds")]
    [InlineData("kg")]
    [InlineData("mph")]
    public void TryNormalizeUnit_InvalidUnits_ReturnsFalse(string input)
    {
        // Act
        var success = this._module.TryNormalizeUnit(input, CultureInfo.InvariantCulture, out var normalized);

        // Assert
        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
    }

    [Theory]
    [InlineData(1.0, "m", "ft", 3.281)] // 1 meter = ~3.281 feet
    [InlineData(1.0, "ft", "m", 0.3048)] // 1 foot = ~0.3048 meters
    [InlineData(1.0, "m", "cm", 100.0)] // 1 meter = 100 centimeters
    [InlineData(100.0, "cm", "m", 1.0)] // 100 centimeters = 1 meter
    [InlineData(1.0, "km", "mi", 0.621)] // 1 kilometer = ~0.621 miles
    [InlineData(1.0, "mi", "km", 1.609)] // 1 mile = ~1.609 kilometers
    [InlineData(12.0, "in", "ft", 1.0)] // 12 inches = 1 foot
    [InlineData(1.0, "ft", "in", 12.0)] // 1 foot = 12 inches
    [InlineData(1.0, "yd", "ft", 3.0)] // 1 yard = 3 feet
    [InlineData(3.0, "ft", "yd", 1.0)] // 3 feet = 1 yard
    [InlineData(1.0, "nmi", "km", 1.852)] // 1 nautical mile = 1.852 kilometers
    public void TryConvert_ValidConversions_ReturnsCorrectResult(
        double value,
        string fromUnit,
        string toUnit,
        double expectedResult)
    {
        // Act
        var success = this._module.TryConvert(value, fromUnit, toUnit, out var result);

        // Assert
        Assert.True(success);
        Assert.Equal(expectedResult, result, 3); // 3 decimal places precision
    }

    [Theory]
    [InlineData(1.0, "invalid", "m")]
    [InlineData(1.0, "m", "invalid")]
    [InlineData(1.0, "°C", "m")] // Temperature unit to length
    [InlineData(1.0, "m", "kg")] // Length to mass
    public void TryConvert_InvalidUnits_ReturnsFalse(double value, string fromUnit, string toUnit)
    {
        // Act
        var success = this._module.TryConvert(value, fromUnit, toUnit, out var result);

        // Assert
        Assert.False(success);
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(1.0, "m")]
    [InlineData(100.0, "cm")]
    [InlineData(1.0, "ft")]
    [InlineData(12.0, "in")]
    [InlineData(1.0, "km")]
    public void GetCommonConversions_ValidUnit_ReturnsSuggestions(double value, string unit)
    {
        // Act
        var suggestions = this._module.GetCommonConversions(value, unit).ToArray();

        // Assert
        Assert.NotEmpty(suggestions);
        Assert.All(suggestions, s =>
        {
            Assert.NotNull(s.Output.Unit.Unit);
            Assert.NotEmpty(s.Output.Unit.Unit);
            Assert.True(s.Output.Value > 0);
            Assert.NotNull(s.FormattedResult);
            Assert.NotEmpty(s.FormattedResult);
        });
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData("°C")]
    public void GetCommonConversions_InvalidUnit_ReturnsEmpty(string unit)
    {
        // Act
        var suggestions = this._module.GetCommonConversions(1.0, unit).ToArray();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSupportedUnits_ReturnsAllExpectedUnits()
    {
        // Act
        var supportedUnits = this._module.GetSupportedUnits();

        // Assert
        Assert.NotEmpty(supportedUnits);

        // Check some key units are present
        var expectedKeys = new[] { "m", "cm", "mm", "km", "ft", "in", "yd", "mi", "nmi" };
        foreach (var key in expectedKeys)
        {
            Assert.True(supportedUnits.ContainsKey(key), $"Missing unit: {key}");
            Assert.NotEmpty(supportedUnits[key]);
        }
    }

    [Theory]
    [InlineData(0.0, "m", "ft")]
    [InlineData(-1.0, "m", "ft")]
    [InlineData(double.MaxValue, "m", "cm")]
    [InlineData(double.MinValue, "m", "cm")]
    public void TryConvert_EdgeCases_HandlesGracefully(double value, string fromUnit, string toUnit)
    {
        // Act
        var success = this._module.TryConvert(value, fromUnit, toUnit, out var result);

        // Assert
        // Should either succeed with a valid result or fail gracefully
        if (success)
        {
            Assert.False(double.IsNaN(result));
            Assert.False(double.IsInfinity(result));
        }
        else
        {
            Assert.Equal(0, result);
        }
    }
}