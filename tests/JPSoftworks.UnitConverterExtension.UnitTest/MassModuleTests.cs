// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class MassModuleTests
{
    private readonly UnitsNetMassModule _module;

    public MassModuleTests()
    {
        _module = new UnitsNetMassModule();
    }

    [Theory]
    [InlineData("ct", "carat")]
    [InlineData("mg", "milligram")]
    [InlineData("cg", "centigram")]
    [InlineData("dg", "decigram")]
    [InlineData("g", "gram")]
    [InlineData("dag", "dekagram")]
    [InlineData("hg", "hectogram")]
    [InlineData("kg", "kilogram")]
    [InlineData("t", "tonne")]
    [InlineData("oz", "ounce")]
    [InlineData("lb", "pound")]
    [InlineData("stone", "stones")]
    [InlineData("short tn", "short ton")]
    public void TryNormalizeUnit_AllSupportedUnits_ReturnsTrue(string unitId, string alias)
    {
        // Act
        var result1 = _module.TryNormalizeUnit(unitId, CultureInfo.InvariantCulture, out var normalized1);
        var result2 = _module.TryNormalizeUnit(alias, CultureInfo.InvariantCulture, out var normalized2);

        // Assert
        Assert.True(result1, $"Unit ID '{unitId}' should be recognized");
        Assert.True(result2, $"Alias '{alias}' should be recognized");
        Assert.Equal(unitId, normalized1);
        Assert.Equal(unitId, normalized2);
    }

    [Theory]
    [InlineData(1, "kg", "g", 1000)]           // 1 kg = 1000 g
    [InlineData(1000, "g", "kg", 1)]           // 1000 g = 1 kg
    [InlineData(1, "kg", "lb", 2.205)]         // 1 kg ≈ 2.205 lb
    [InlineData(1, "lb", "kg", 0.454)]         // 1 lb ≈ 0.454 kg
    [InlineData(1, "lb", "oz", 16)]            // 1 lb = 16 oz
    [InlineData(16, "oz", "lb", 1)]            // 16 oz = 1 lb
    [InlineData(1, "stone", "lb", 14)]         // 1 stone = 14 lb
    [InlineData(14, "lb", "stone", 1)]         // 14 lb = 1 stone
    [InlineData(1, "t", "kg", 1000)]           // 1 tonne = 1000 kg
    [InlineData(1000, "kg", "t", 1)]           // 1000 kg = 1 tonne
    [InlineData(1, "short tn", "lb", 2000)]    // 1 short ton = 2000 lb
    [InlineData(2000, "lb", "short tn", 1)]    // 2000 lb = 1 short ton
    [InlineData(1, "g", "mg", 1000)]           // 1 g = 1000 mg
    [InlineData(1000, "mg", "g", 1)]           // 1000 mg = 1 g
    [InlineData(1, "g", "cg", 100)]            // 1 g = 100 cg
    [InlineData(100, "cg", "g", 1)]            // 100 cg = 1 g
    [InlineData(1, "g", "dg", 10)]             // 1 g = 10 dg
    [InlineData(10, "dg", "g", 1)]             // 10 dg = 1 g
    [InlineData(1, "dag", "g", 10)]            // 1 dag = 10 g
    [InlineData(10, "g", "dag", 1)]            // 10 g = 1 dag
    [InlineData(1, "hg", "g", 100)]            // 1 hg = 100 g
    [InlineData(100, "g", "hg", 1)]            // 100 g = 1 hg
    [InlineData(1, "ct", "g", 0.2)]            // 1 carat = 0.2 g
    [InlineData(5, "ct", "g", 1)]              // 5 carats = 1 g
    [InlineData(1, "g", "ct", 5)]              // 1 g = 5 carats
    public void TryConvert_CommonConversions_ReturnsCorrectResults(
        double inputValue, 
        string fromUnit, 
        string toUnit, 
        double expectedValue)
    {
        // Act
        var result = _module.TryConvert(inputValue, fromUnit, toUnit, out var convertedValue);

        // Assert
        Assert.True(result, $"Conversion from {fromUnit} to {toUnit} should succeed");
        Assert.Equal(expectedValue, convertedValue, 3); // 3 decimal places precision
    }

    [Theory]
    [InlineData("ct", "Carat", "ct", "Metric")]
    [InlineData("mg", "Milligram", "mg", "Metric")]
    [InlineData("cg", "Centigram", "cg", "Metric")]
    [InlineData("dg", "Decigram", "dg", "Metric")]
    [InlineData("g", "Gram", "g", "Metric")]
    [InlineData("dag", "Dekagram", "dag", "Metric")]
    [InlineData("hg", "Hectogram", "hg", "Metric")]
    [InlineData("kg", "Kilogram", "kg", "Metric")]
    [InlineData("t", "Metric Tonne", "t", "Metric")]
    [InlineData("oz", "Ounce", "oz", "Imperial")]
    [InlineData("lb", "Pound", "lb", "Imperial")]
    [InlineData("stone", "Stone", "st", "Imperial")]
    [InlineData("short tn", "Short Ton", "short tn", "US")]
    public void GetUnitMetadata_AllUnits_ReturnsCorrectMetadata(
        string unitId, 
        string expectedDisplayName, 
        string expectedSymbol, 
        string expectedSystem)
    {
        // Act
        var metadata = _module.GetUnitMetadata(unitId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(expectedDisplayName, metadata.DisplayName);
        Assert.Equal(expectedSymbol, metadata.Symbol);
        Assert.Equal(expectedSystem, metadata.System);
        Assert.True(metadata.Tags.Length > 0);
        Assert.NotEmpty(metadata.Description);
    }

    [Theory]
    [InlineData("kg", true)]    // Common
    [InlineData("g", true)]     // Common
    [InlineData("lb", true)]    // Common
    [InlineData("oz", true)]    // Common
    [InlineData("t", true)]     // Common
    [InlineData("short tn", true)] // Common
    [InlineData("mg", true)]    // Common (medicine)
    [InlineData("ct", false)]   // Specialized (jewelry)
    [InlineData("cg", false)]   // Less common
    [InlineData("dg", false)]   // Less common
    [InlineData("dag", false)]  // Less common
    [InlineData("hg", false)]   // Less common
    [InlineData("stone", false)] // Regional (UK)
    public void GetUnitMetadata_CommonalityFlags_AreCorrect(string unitId, bool expectedIsCommon)
    {
        // Act
        var metadata = _module.GetUnitMetadata(unitId);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(expectedIsCommon, metadata.IsCommon);
    }

    [Theory]
    [InlineData(1, "kg")]     // Should suggest g, lb
    [InlineData(100, "g")]    // Should suggest kg, oz
    [InlineData(1, "lb")]     // Should suggest kg, oz
    [InlineData(10, "stone")] // Should suggest kg, lb
    [InlineData(5, "ct")]     // Should suggest g, mg
    [InlineData(1, "t")]      // Should suggest kg, short tn
    public void GetCommonConversions_ValidInput_ReturnsSuggestions(double value, string unit)
    {
        // Act
        var suggestions = _module.GetCommonConversions(value, unit).ToArray();

        // Assert
        Assert.True(suggestions.Length > 0, "Should return at least one suggestion");
        Assert.All(suggestions, suggestion =>
        {
            Assert.True(suggestion.Output.Value > 0);
            Assert.NotEmpty(suggestion.Output.Unit.Unit);
            Assert.NotEmpty(suggestion.FormattedResult);
            Assert.NotNull(suggestion.Output.Unit.Metadata);
        });
    }

    [Theory]
    [InlineData("invalid_unit")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    public void TryNormalizeUnit_InvalidUnits_ReturnsFalse(string invalidUnit)
    {
        // Act
        var result = _module.TryNormalizeUnit(invalidUnit, CultureInfo.InvariantCulture, out var normalized);

        // Assert
        Assert.False(result);
        Assert.Empty(normalized);
    }

    [Theory]
    [InlineData(1, "kg", "invalid_unit")]
    [InlineData(1, "invalid_unit", "kg")]
    public void TryConvert_InvalidUnits_ReturnsFalse(double value, string fromUnit, string toUnit)
    {
        // Act
        var result = _module.TryConvert(value, fromUnit, toUnit, out var convertedValue);

        // Assert
        Assert.False(result);
        Assert.Equal(0, convertedValue);
    }

    [Fact]
    public void GetAllUnitMetadata_ReturnsAllSupportedUnits()
    {
        // Act
        var allMetadata = _module.GetAllUnitMetadata();

        // Assert
        var expectedUnits = new[] { "ct", "mg", "cg", "dg", "g", "dag", "hg", "kg", "t", "oz", "lb", "stone", "short tn" };
        
        Assert.Equal(expectedUnits.Length, allMetadata.Count);
        
        foreach (var expectedUnit in expectedUnits)
        {
            Assert.True(allMetadata.ContainsKey(expectedUnit), $"Should contain unit: {expectedUnit}");
            Assert.NotNull(allMetadata[expectedUnit]);
            Assert.NotEmpty(allMetadata[expectedUnit].DisplayName);
        }
    }

    [Fact]
    public void ModuleProperties_AreCorrect()
    {
        // Assert
        Assert.Equal("Mass", _module.Category);
        Assert.False(_module.SupportsSquaredUnits);
        Assert.False(_module.SupportsCubicUnits);
    }

    [Theory]
    [InlineData("kilogram", "kg")]
    [InlineData("kilograms", "kg")]
    [InlineData("gram", "g")]
    [InlineData("grams", "g")]
    [InlineData("pound", "lb")]
    [InlineData("pounds", "lb")]
    [InlineData("lbs", "lb")]
    [InlineData("ounce", "oz")]
    [InlineData("ounces", "oz")]
    [InlineData("milligram", "mg")]
    [InlineData("milligrams", "mg")]
    [InlineData("carat", "ct")]
    [InlineData("carats", "ct")]
    [InlineData("centigram", "cg")]
    [InlineData("centigrams", "cg")]
    [InlineData("decigram", "dg")]
    [InlineData("decigrams", "dg")]
    [InlineData("dekagram", "dag")]
    [InlineData("dekagrams", "dag")]
    [InlineData("decagram", "dag")]    // Alternative spelling
    [InlineData("decagrams", "dag")]   // Alternative spelling
    [InlineData("hectogram", "hg")]
    [InlineData("hectograms", "hg")]
    [InlineData("tonne", "t")]
    [InlineData("tonnes", "t")]
    [InlineData("metric ton", "t")]
    [InlineData("metric tons", "t")]
    [InlineData("stones", "stone")]
    [InlineData("short ton", "short tn")]
    [InlineData("short tons", "short tn")]
    [InlineData("US ton", "short tn")]
    [InlineData("US tons", "short tn")]
    [InlineData("ton", "short tn")]
    [InlineData("tons", "short tn")]
    public void TryNormalizeUnit_AllAliases_WorkCorrectly(string alias, string expectedUnitId)
    {
        // Act
        var result = _module.TryNormalizeUnit(alias, CultureInfo.InvariantCulture, out var normalized);

        // Assert
        Assert.True(result, $"Alias '{alias}' should be recognized");
        Assert.Equal(expectedUnitId, normalized);
    }

    [Theory]
    [InlineData(0.0)]     // Zero value
    [InlineData(-1.0)]    // Negative value (should still convert mathematically)
    [InlineData(0.001)]   // Very small value
    [InlineData(1000000)] // Very large value
    public void TryConvert_EdgeCases_HandlesCorrectly(double edgeValue)
    {
        // Act
        var result = _module.TryConvert(edgeValue, "kg", "g", out var convertedValue);

        // Assert
        Assert.True(result);
        Assert.Equal(edgeValue * 1000, convertedValue, 6); // kg to g is *1000
        Assert.False(double.IsNaN(convertedValue));
        Assert.False(double.IsInfinity(convertedValue));
    }
}