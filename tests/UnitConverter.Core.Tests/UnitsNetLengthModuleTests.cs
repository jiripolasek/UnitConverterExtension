// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class UnitsNetLengthModuleTests
{
    private readonly UnitsNetLengthModule _module;

    public UnitsNetLengthModuleTests()
    {
        this._module = new UnitsNetLengthModule();
    }

    [Fact]
    public void PropertiesReturnsCorrectValues()
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
    public void TryNormalizeUnitIdsSymbolsAndAliasesNormalizeToId(string input, string expected)
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
    public void TryNormalizeUnitInvalidUnitsReturnsFalse(string input)
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
    public void TryConvertValidConversionsReturnsCorrectResult(
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
    public void TryConvertInvalidUnitsReturnsFalse(double value, string fromUnit, string toUnit)
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
    public void GetCommonConversionsValidUnitReturnsSuggestions(double value, string unit)
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
    public void GetCommonConversionsInvalidUnitReturnsEmpty(string unit)
    {
        // Act
        var suggestions = this._module.GetCommonConversions(1.0, unit).ToArray();

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void GetSupportedUnitsReturnsAllExpectedUnits()
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
    public void TryConvertEdgeCasesHandlesGracefully(double value, string fromUnit, string toUnit)
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

    [Theory]
    [InlineData("1 m to data mile", "dmi", 1.0 / 1828.8)]
    [InlineData("1 data mile to m", "m", 1828.8)]
    [InlineData("1 m to µm", "um", 1_000_000.0)]
    [InlineData("1 nm to angstrom", "angstrom", 10.0)]
    [InlineData("1 hand to cm", "cm", 10.16)]
    [InlineData("1 dtp point to in", "in", 1.0 / 72.0)]
    [InlineData("1 Mm to m", "m", 1_000_000.0)]
    [InlineData("1 m to Mm", "Mm", 0.000001)]
    public void ConvertQueryCanonicalLengthIdsUseCorrectScale(string query, string unit, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(unit, result.Output.Unit.Unit);
        Assert.Equal(expected, result.Output.Value, 10);
    }

    [Theory]
    [InlineData("72 dtp pt to in", "in", 1)]
    [InlineData("6 dtp pica to in", "in", 1)]
    [InlineData("1 dtp pica to dtp pt", "dtp pt", 12)]
    [InlineData("72.27 printer pt to in", "in", 1)]
    [InlineData("6.0225 printer pica to in", "in", 1)]
    [InlineData("1 printer pica to printer pt", "printer pt", 12)]
    [InlineData("1440 twip to in", "in", 1)]
    public void ConvertQueryTypographicUnitsUseTheirDistinctDefinitions(string query, string unit, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(unit, result.Output.Unit.Unit);
        Assert.Equal(expected, result.Output.Value, 10);
    }

    [Theory]
    [InlineData("96 px to in", "in", 1)]
    [InlineData("16 px to dtp pt", "dtp pt", 12)]
    [InlineData("12 css pt to px", "px", 16)]
    [InlineData("1 css pc to px", "px", 16)]
    [InlineData("1 in to px", "px", 96)]
    [InlineData("2.54 cm to px", "px", 96)]
    [InlineData("25.4 mm to px", "px", 96)]
    [InlineData("4 Q to mm", "mm", 1)]
    [InlineData("1 q to px", "px", 96.0 / 101.6)]
    [InlineData("1 in to Q", "Q", 101.6)]
    [InlineData("1 px to mm", "mm", 25.4 / 96)]
    [InlineData("1 pt DTP to px", "px", 4.0 / 3)]
    [InlineData("1 pt (CSS) to px", "px", 4.0 / 3)]
    [InlineData("1 pica DTP to px", "px", 16)]
    [InlineData("1 pc (CSS) to px", "px", 16)]
    [InlineData("1 inch to pixels", "px", 96)]
    public void ConvertQueryCssAbsoluteLengthsUseReferencePixels(string query, string unit, double expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(UnitType.Length, result.Output.Unit.Type);
        Assert.Equal(unit, result.Output.Unit.Unit);
        Assert.Equal(expected, result.Output.Value, 10);
    }

    [Theory]
    [InlineData(VolumeSystemPreference.US, "us pt")]
    [InlineData(VolumeSystemPreference.Imperial, "imp pt")]
    public void BarePtKeepsPintAndOffersDtpPoints(VolumeSystemPreference preference, string pint)
    {
        var service = new ConversionService(new DefaultUnitPreferenceProvider(preference));
        var result = service.ConvertQuery("1 pt", CultureInfo.GetCultureInfo("en-US"));

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(UnitType.Volume, result.Output.Unit.Type);
        Assert.Equal(pint, result.Output.Unit.Unit);
        var alternative = Assert.Single(result.OtherInterpretations, item => item.Query == "1 dtp pt");
        var points = service.ConvertQuery(alternative.Query, CultureInfo.GetCultureInfo("en-US"));
        Assert.True(points.Success, points.ErrorMessage);
        Assert.Equal(UnitType.Length, points.Output.Unit.Type);
        Assert.Equal("dtp pt", points.Output.Unit.Unit);
        Assert.Equal(1, points.Output.Value);
    }

    [Fact]
    public void BarePcKeepsParsecsAndOffersCssPicas()
    {
        var service = new ConversionService();
        var result = service.ConvertQuery("1 pc", CultureInfo.InvariantCulture);

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("pc", result.Output.Unit.Unit);
        var alternative = Assert.Single(result.OtherInterpretations, item => item.Query == "1 dtp pica");
        Assert.True(service.ConvertQuery(alternative.Query, CultureInfo.InvariantCulture).Success);
    }

    [Theory]
    [InlineData("en-US", "meter")]
    [InlineData("ru-RU", "meter")]
    [InlineData("ru-RU", "м")]
    [InlineData("zh-CN", "meter")]
    [InlineData("zh-CN", "米")]
    public void ConvertQueryLocalizedUnitsKeepCanonicalIds(string cultureName, string input)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        Assert.True(this._module.TryNormalizeUnit(input, culture, out var normalized));
        Assert.Equal("m", normalized);

        var result = new ConversionService().ConvertQuery($"1 {input} -> feet", culture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1.0 / 0.3048, result.Output.Value, 10);
    }

    [Theory]
    [InlineData("1 h", UnitType.Time, "h")]
    [InlineData("1 pt", UnitType.Volume, "us pt")]
    public void ConvertQueryReservedAbbreviationsKeepOriginalCategory(string query, UnitType type, string unit)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.GetCultureInfo("en-US"));

        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(type, result.Output.Unit.Type);
        Assert.Equal(unit, result.Output.Unit.Unit);
        Assert.NotEmpty(result.Suggestions);
    }

    [Theory]
    [InlineData("en-US", "Millimeter", "Megameter", "Decimeter", "Data Mile")]
    [InlineData("ru-RU", "Millimeter", "Megameter", "Decimeter", "Data Mile")]
    [InlineData("zh-CN", "毫米", "兆米", "分米", "数据英里")]
    public void GetAllUnitMetadataPreservesCaseSensitiveIds(string cultureName, string millimeter, string megameter, string decimeter, string dataMile)
    {
        var metadata = this._module.GetAllUnitMetadata(CultureInfo.GetCultureInfo(cultureName));

        Assert.Equal(this._module.GetSupportedUnits().Count, metadata.Count);
        Assert.Equal(millimeter, metadata["mm"].DisplayName);
        Assert.Equal(megameter, metadata["Mm"].DisplayName);
        Assert.Equal(decimeter, metadata["dm"].DisplayName);
        Assert.Equal(dataMile, metadata["dmi"].DisplayName);
    }

    [Theory]
    [InlineData("decimeters", "dm", "cm")]
    [InlineData("parsecs", "pc", "ly")]
    [InlineData("fathoms", "fathom", "m")]
    [InlineData("survey feet", "us survey ft", "m")]
    [InlineData("microinches", "microinch", "mil")]
    [InlineData("dekametre", "dam", "m")]
    public void LegacyAliasesRetainConversionsAndSuggestions(string alias, string id, string target)
    {
        Assert.True(this._module.TryNormalizeUnit(alias, CultureInfo.InvariantCulture, out var normalized));
        Assert.Equal(id, normalized);
        Assert.True(this._module.TryConvert(10, alias, target, out _));
        Assert.Contains(this._module.GetCommonConversions(10, alias), suggestion => suggestion.Output.Unit.Unit == target);
    }

    [Fact]
    public void TryConvertAllSupportedUnitsConvertToAndFromMeters()
    {
        foreach (var unit in this._module.GetSupportedUnits().Keys)
        {
            Assert.True(this._module.TryConvert(1, unit, "m", out var meters), $"Cannot convert {unit} to meters.");
            Assert.True(this._module.TryConvert(meters, "m", unit, out var original), $"Cannot convert meters to {unit}.");
            Assert.Equal(1, original, 10);
        }
    }
}