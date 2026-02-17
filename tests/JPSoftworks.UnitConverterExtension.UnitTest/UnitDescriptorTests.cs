// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class UnitDescriptorTests
{
    [Fact]
    public void SpeedModule_WithIdDescriptors_WorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetSpeedModule();
        var supportedUnits = module.GetSupportedUnits();

        // Assert
        Assert.Contains("km/h", supportedUnits.Keys);

        // Act
        var metadata = module.GetUnitMetadata("km/h");
        
        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Kilometer per hour", metadata.DisplayName);
        Assert.Equal("Metric", metadata.System);

        // Act
        var normalizeResult = module.TryNormalizeUnit("km/h", CultureInfo.InvariantCulture, out var normalized);
        
        // Assert
        Assert.True(normalizeResult);
        Assert.Equal("km/h", normalized);

        // Act
        var conversionResult = module.TryConvert(100, "km/h", "mph", out var result);
        
        // Assert
        Assert.True(conversionResult);
        Assert.Equal(62.137, result, 3); // 100 km/h ≈ 62.137 mph

        // Act
        var suggestions = module.GetCommonConversions(100, "km/h").ToArray();
        
        // Assert
        Assert.NotEmpty(suggestions);
        Assert.Contains(suggestions, s => s.Output.Unit.Unit == "mph");
    }

    [Fact]
    public void SpeedModule_WithSymbolAndAlias_NormalizesToId()
    {
        // Arrange
        var module = new UnitsNetSpeedModule();

        // Act
        var symbolNormalized = module.TryNormalizeUnit("km/h", CultureInfo.InvariantCulture, out var symbolId);
        var aliasNormalized = module.TryNormalizeUnit("kilometers per hour", CultureInfo.InvariantCulture, out var aliasId);

        // Assert
        Assert.True(symbolNormalized);
        Assert.True(aliasNormalized);
        Assert.Equal("km/h", symbolId);
        Assert.Equal("km/h", aliasId);
    }

    [Fact]
    public void PowerModule_WithIdDescriptors_WorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetPowerModule();

        // Act
        var metadata = module.GetUnitMetadata("kW");
        
        // Assert
        Assert.NotNull(metadata);
        Assert.Equal("Kilowatt", metadata.DisplayName);
        Assert.Equal("kW", metadata.Symbol);
        Assert.Contains("electrical", metadata.Tags);

        // Act
        var allMetadata = module.GetAllUnitMetadata();
        
        // Assert
        Assert.Contains("W", allMetadata.Keys);
        Assert.Contains("hp", allMetadata.Keys);

        // Act
        var conversionResult = module.TryConvert(1, "kW", "hp", out var result);
        
        // Assert
        Assert.True(conversionResult);
        Assert.Equal(1.341, result, 3); // 1 kW ≈ 1.341 hp
    }

    [Fact]
    public void PowerModule_WithSymbol_NormalizesToId()
    {
        // Arrange
        var module = new UnitsNetPowerModule();

        // Act
        var symbolNormalized = module.TryNormalizeUnit("kW", CultureInfo.InvariantCulture, out var symbolId);

        // Assert
        Assert.True(symbolNormalized);
        Assert.Equal("kW", symbolId);
    }

    [Theory]
    [InlineData("Length", "m")] 
    [InlineData("Temperature", "°C")] 
    [InlineData("Speed", "km/h")] 
    [InlineData("Power", "W")] 
    [InlineData("Energy", "J")] 
    [InlineData("Mass", "kg")] 
    [InlineData("Volume", "l")] 
    [InlineData("Pressure", "Pa")] 
    [InlineData("Angle", "deg")] 
    [InlineData("Data", "B")] 
    [InlineData("Time", "s")] 
    public void AllModules_WithDescriptors_ProvideRichMetadata(string category, string unitId)
    {
        // Arrange
        var service = new ConversionService();
        var allUnits = service.GetAllSupportedUnits();

        // Assert
        Assert.True(allUnits.ContainsKey(category), $"Missing category: {category}");
        var categoryUnits = allUnits[category];
        Assert.True(categoryUnits.ContainsKey(unitId), $"Category {category} should contain unit {unitId}");
    }

    [Theory]
    [InlineData("m", "m")]
    [InlineData("°C", "°C")]
    [InlineData("km/h", "km/h")]
    [InlineData("kW", "kW")]
    [InlineData("J", "J")]
    [InlineData("kg", "kg")]
    [InlineData("l", "l")]
    [InlineData("Pa", "Pa")]
    [InlineData("deg", "deg")]
    [InlineData("B", "B")]
    [InlineData("s", "s")]
    public void AllModules_WithSymbolsAndAliases_NormalizeToIds(string input, string expectedId)
    {
        // Arrange
        var service = new ConversionService();

        // Act
        var normalized = NormalizeUnit(service.Modules, input);

        // Assert
        Assert.Equal(expectedId, normalized);
    }

    private static string NormalizeUnit(IEnumerable<IConversionModule> modules, string unit)
    {
        foreach (var module in modules)
        {
            if (module.TryNormalizeUnit(unit, CultureInfo.InvariantCulture, out var normalized))
            {
                return normalized;
            }
        }

        return string.Empty;
    }
}