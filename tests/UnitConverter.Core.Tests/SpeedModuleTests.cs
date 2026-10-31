// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class SpeedModuleTests
{
    private readonly UnitsNetSpeedModule _module = new();
    private readonly ConversionService _service = new();

    private static CultureInfo ResolveCulture(string name) =>
        string.IsNullOrEmpty(name) ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(name);

    // ── Properties ─────────────────────────────────────────────

    [Fact]
    public void PropertiesReturnsCorrectValues()
    {
        Assert.Equal("Speed", _module.Category);
        Assert.False(_module.SupportsSquaredUnits);
        Assert.False(_module.SupportsCubicUnits);
    }

    // ── Normalization: IDs, symbols, and aliases ───────────────

    [Theory]
    // IDs - invariant culture
    [InlineData("", "m/s", "m/s")]
    [InlineData("", "km/h", "km/h")]
    [InlineData("", "ft/s", "ft/s")]
    [InlineData("", "mph", "mph")]
    [InlineData("", "kn", "kn")]
    [InlineData("", "cm/s", "cm/s")]
    [InlineData("", "Mach", "Mach")]
    // Aliases - invariant culture
    [InlineData("", "meter per second", "m/s")]
    [InlineData("", "meters per second", "m/s")]
    [InlineData("", "metre per second", "m/s")]
    [InlineData("", "metres per second", "m/s")]
    [InlineData("", "mps", "m/s")]
    [InlineData("", "kilometers per hour", "km/h")]
    [InlineData("", "kilometres per hour", "km/h")]
    [InlineData("", "kph", "km/h")]
    [InlineData("", "kmh", "km/h")]
    [InlineData("", "feet per second", "ft/s")]
    [InlineData("", "foot per second", "ft/s")]
    [InlineData("", "fps", "ft/s")]
    [InlineData("", "miles per hour", "mph")]
    [InlineData("", "knot", "kn")]
    [InlineData("", "knots", "kn")]
    [InlineData("", "kt", "kn")]
    [InlineData("", "kts", "kn")]
    [InlineData("", "centimeters per second", "cm/s")]
    [InlineData("", "centimetres per second", "cm/s")]
    [InlineData("", "cmps", "cm/s")]
    [InlineData("", "mach", "Mach")]
    // Cross-culture consistency
    [InlineData("en-US", "mph", "mph")]
    [InlineData("de-DE", "mph", "mph")]
    [InlineData("fr-FR", "km/h", "km/h")]
    [InlineData("cs-CZ", "m/s", "m/s")]
    [InlineData("ja-JP", "knot", "kn")]
    [InlineData("en-US", "knots", "kn")]
    [InlineData("de-DE", "kph", "km/h")]
    [InlineData("fr-FR", "mps", "m/s")]
    [InlineData("cs-CZ", "feet per second", "ft/s")]
    public void TryNormalizeUnitIdsSymbolsAndAliasesNormalizeToId(string culture, string input, string expected)
    {
        var success = _module.TryNormalizeUnit(input, ResolveCulture(culture), out var normalized);

        Assert.True(success, $"Expected '{input}' to normalize for culture '{culture}', but it did not.");
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("", "invalid")]
    [InlineData("", "")]
    [InlineData("", "   ")]
    [InlineData("", "°C")]
    [InlineData("", "kg")]
    [InlineData("", "meters")]
    [InlineData("de-DE", "invalid")]
    [InlineData("fr-FR", "°C")]
    public void TryNormalizeUnitInvalidUnitsReturnsFalse(string culture, string input)
    {
        var success = _module.TryNormalizeUnit(input, ResolveCulture(culture), out var normalized);

        Assert.False(success);
        Assert.Equal(string.Empty, normalized);
    }

    // ── Conversion correctness ─────────────────────────────────

    [Theory]
    [InlineData(100.0, "km/h", "mph", 62.137, 3)]
    [InlineData(30.0, "m/s", "ft/s", 98.4252, 4)]
    [InlineData(10.0, "kn", "km/h", 18.52, 2)]
    [InlineData(60.0, "mph", "km/h", 96.561, 3)]
    [InlineData(1.0, "m/s", "km/h", 3.6, 1)]
    [InlineData(100.0, "cm/s", "m/s", 1.0, 1)]
    public void TryConvertValidConversionsReturnsCorrectResult(
        double value, string fromUnit, string toUnit, double expectedResult, int precision)
    {
        var success = _module.TryConvert(value, fromUnit, toUnit, out var result);

        Assert.True(success);
        Assert.Equal(expectedResult, result, precision);
    }

    [Theory]
    [InlineData(1.0, "invalid", "m/s")]
    [InlineData(1.0, "m/s", "invalid")]
    [InlineData(1.0, "°C", "m/s")]
    [InlineData(1.0, "m/s", "kg")]
    public void TryConvertInvalidUnitsReturnsFalse(double value, string fromUnit, string toUnit)
    {
        var success = _module.TryConvert(value, fromUnit, toUnit, out var result);

        Assert.False(success);
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(0.0, "m/s", "km/h")]
    [InlineData(-1.0, "m/s", "km/h")]
    [InlineData(double.MaxValue, "m/s", "km/h")]
    [InlineData(double.MinValue, "m/s", "km/h")]
    public void TryConvertEdgeCasesHandlesGracefully(double value, string fromUnit, string toUnit)
    {
        var success = _module.TryConvert(value, fromUnit, toUnit, out var result);

        if (success)
        {
            Assert.False(double.IsNaN(result));
            Assert.False(double.IsInfinity(result));
        }
    }

    // ── Common conversions ─────────────────────────────────────

    [Theory]
    [InlineData(100.0, "km/h")]
    [InlineData(30.0, "m/s")]
    [InlineData(60.0, "mph")]
    [InlineData(10.0, "kn")]
    public void GetCommonConversionsValidUnitReturnsSuggestions(double value, string unit)
    {
        var suggestions = _module.GetCommonConversions(value, unit).ToArray();

        Assert.NotEmpty(suggestions);
        Assert.All(suggestions, s =>
        {
            Assert.NotNull(s.Output.Unit.Unit);
            Assert.NotEmpty(s.Output.Unit.Unit);
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
        var suggestions = _module.GetCommonConversions(1.0, unit).ToArray();

        Assert.Empty(suggestions);
    }

    // ── Supported units ────────────────────────────────────────

    [Fact]
    public void GetSupportedUnitsReturnsAllExpectedUnits()
    {
        var supportedUnits = _module.GetSupportedUnits();

        Assert.NotEmpty(supportedUnits);

        var expectedKeys = new[] { "m/s", "km/h", "ft/s", "mph", "kn", "cm/s", "Mach" };
        foreach (var key in expectedKeys)
        {
            Assert.True(supportedUnits.ContainsKey(key), $"Missing unit: {key}");
        }
    }

    // ── Metadata ───────────────────────────────────────────────

    [Theory]
    [InlineData("m/s", "Meter per second", "m/s")]
    [InlineData("km/h", "Kilometer per hour", "km/h")]
    [InlineData("mph", "Mile per hour", "mph")]
    [InlineData("kn", "Knot", "kn")]
    public void GetUnitMetadataReturnsExpectedDisplayNameAndSymbol(
        string unitId, string expectedDisplayName, string expectedSymbol)
    {
        var metadata = _module.GetUnitMetadata(unitId);

        Assert.NotNull(metadata);
        Assert.Equal(expectedDisplayName, metadata.DisplayName);
        Assert.Equal(expectedSymbol, metadata.Symbol);
    }

    // ── End-to-end query with culture ──────────────────────────

    [Theory]
    // en-US (decimal point)
    [InlineData("en-US", "100 km/h to mph", 62.137, "mph", 3)]
    [InlineData("en-US", "30 m/s to ft/s", 98.4252, "ft/s", 4)]
    [InlineData("en-US", "10 kn to km/h", 18.52, "km/h", 2)]
    [InlineData("en-US", "1.5 m/s to km/h", 5.4, "km/h", 1)]
    // de-DE (decimal comma)
    [InlineData("de-DE", "100,5 km/h in mph", 62.448, "mph", 3)]
    [InlineData("de-DE", "1,5 m/s in km/h", 5.4, "km/h", 1)]
    // fr-FR (decimal comma)
    [InlineData("fr-FR", "100,5 km/h en mph", 62.448, "mph", 3)]
    [InlineData("fr-FR", "1,5 m/s en km/h", 5.4, "km/h", 1)]
    // cs-CZ (decimal comma)
    [InlineData("cs-CZ", "100,5 km/h na mph", 62.448, "mph", 3)]
    public void ConvertQueryWithCultureParsesAndConverts(
        string cultureName, string query, double expectedValue, string expectedUnit, int precision)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var result = _service.ConvertQuery(query, culture);

        Assert.True(result.Success, $"Query '{query}' failed for culture '{cultureName}'.");
        Assert.Equal(expectedUnit, result.Output.Unit.Unit);
        Assert.Equal(expectedValue, result.Output.Value, precision);
    }
}