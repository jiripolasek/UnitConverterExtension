// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

/// <summary>
///     Comprehensive integration tests that combine parser, modules, and service functionality
/// </summary>
public class IntegrationTests
{
    private readonly ConversionService _service;

    public IntegrationTests()
    {
        this._service = new ConversionService();
    }

    [Theory]
    [InlineData("1 m to ft", true, 3.281, "ft")]
    [InlineData("100 cm to m", true, 1.0, "m")]
    [InlineData("1 km to mi", true, 0.621, "mi")]
    [InlineData("12 in to ft", true, 1.0, "ft")]
    [InlineData("0 °C to °F", true, 32.0, "°F")]
    [InlineData("100 °C to °F", true, 212.0, "°F")]
    [InlineData("60 min to h", true, 1.0, "h")]
    [InlineData("1 kg to lb", true, 2.205, "lb")]
    [InlineData("1000 ml to l", true, 1.0, "l")]
    [InlineData("5 ct to g", true, 1.0, "g")]           // 5 carats = 1 gram
    [InlineData("1 dag to g", true, 10.0, "g")]         // 1 dekagram = 10 grams
    [InlineData("1 t to kg", true, 1000.0, "kg")]       // 1 tonne = 1000 kg
    [InlineData("1 short tn to lb", true, 2000.0, "lb")] // 1 short ton = 2000 pounds
    [InlineData("100 cg to g", true, 1.0, "g")]         // 100 centigrams = 1 gram
    [InlineData("1 stone to lb", true, 14.0, "lb")]     // 1 stone = 14 pounds
    [InlineData("2 stone to kg", true, 12.701, "kg")]   // 2 stone ≈ 12.701 kg (2 * 14 * 0.454)
    [InlineData("10 stone to lb", true, 140.0, "lb")]   // 10 stone = 140 pounds
    [InlineData("8 kb to B", true, 1000.0, "B")]        // 8 kilobits = 1000 bytes
    [InlineData("8 kB to kb", true, 64.0, "kb")]        // 8 kilobytes = 64 kilobits
    [InlineData("1 Mb to kb", true, 1000.0, "kb")]      // 1 megabit = 1000 kilobits
    public void EndToEndConversionsCommonCasesWorkCorrectly(
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
        else
        {
            Assert.NotEmpty(result.ErrorMessage);
        }
    }

    [Theory]
    [InlineData("10m in ft", "10m to ft")]
    [InlineData("10 m IN ft", "10 m to ft")]
    [InlineData("10 m\tin\tft", "10 m to ft")]
    [InlineData("12 in in ft", "12 in to ft")]
    [InlineData("1 m in in", "1 m to in")]
    [InlineData("1 cm2 in in2", "1 cm2 to in2")]
    [InlineData("1 ft 2 in in cm", "1 ft 2 in to cm")]
    [InlineData("1 ft (1 + 2) in in cm", "1 ft (1 + 2) in to cm")]
    [InlineData("5'11\" in cm", "5'11\" to cm")]
    [InlineData("5ft11in in cm", "5ft11in to cm")]
    [InlineData("180 cm in ft in", "180 cm to ft in")]
    [InlineData("1 Pa in in Hg", "1 Pa to in Hg")]
    [InlineData("1 in Hg in Pa", "1 in Hg to Pa")]
    [InlineData("1 lbf in in Nm", "1 lbf in to Nm")]
    [InlineData("1 Nm in lbf in", "1 Nm to lbf in")]
    [InlineData("1 lbf·in in Nm", "1 lbf·in to Nm")]
    public void InConnectorMatchesToConnector(string query, string canonical)
    {
        var expected = this._service.ConvertQuery(canonical, CultureInfo.InvariantCulture);
        var actual = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(expected.Success, expected.ErrorMessage);
        Assert.True(actual.Success, actual.ErrorMessage);
        Assert.Equal(expected.Output.Value, actual.Output.Value, 8);
        Assert.Equal(expected.Output.Unit.Unit, actual.Output.Unit.Unit);
        Assert.Equal(expected.FormattedResult, actual.FormattedResult);
    }

    [Theory]
    [InlineData("60 mins")]
    [InlineData("10 in")]
    [InlineData("1 ft 2 in")]
    [InlineData("1 ft 2 in 3 ft")]
    [InlineData("1 in Hg")]
    [InlineData("1 lbf in")]
    [InlineData("1 lbf in 2 lbf in")]
    [InlineData("100 cm")]
    [InlineData("25 °C")]
    [InlineData("2.5 kg")]
    [InlineData("1000 ml")]
    [InlineData("10 stone")]      // Stone implicit conversion
    [InlineData("5 stones")]     // Plural stone
    [InlineData("1 metric ton")] // Multi-word unit
    [InlineData("3 short tons")] // Multi-word plural unit
    public void ImplicitConversionsReturnSuggestions(string query)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.Success, $"Query '{query}' should succeed. Error: {result.ErrorMessage}");
        Assert.NotEmpty(result.Suggestions);
        Assert.All(result.Suggestions, suggestion =>
        {
            Assert.NotEmpty(suggestion.Output.Unit.Unit);
            Assert.True(suggestion.Output.Value > 0);
            Assert.NotEmpty(suggestion.FormattedResult);
        });
    }

    [Theory]
    [InlineData("invalid query")]
    [InlineData("1 m to kg")] // Different categories
    [InlineData("1 m in kg")]
    [InlineData("1 lbf in kg")]
    [InlineData("1 unknown_unit in m")]
    [InlineData("1 ft 2 in cm")]
    [InlineData("1 ft 3 in cm")]
    [InlineData("1 unknown_unit to m")]
    public void InvalidQueriesReturnFailureWithMessage(string query)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.ErrorMessage);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void AllModulesHaveExpectedCategories()
    {
        // Act
        var supportedUnits = this._service.GetAllSupportedUnits();

        // Assert
        var expectedCategories = new[] { "Length", "Temperature", "Time", "Volume", "Mass" };
        foreach (var category in expectedCategories)
        {
            Assert.True(supportedUnits.ContainsKey(category), $"Missing category: {category}");
            Assert.NotEmpty(supportedUnits[category]);
        }
    }

    [Theory]
    [InlineData("1,5 m to ft")] // European decimal
    [InlineData("1.5 m to ft")] // US decimal
    [InlineData("-5 °C to °F")] // Negative
    [InlineData("0 m to ft")] // Zero
    public void DifferentInputFormatsHandleCorrectly(string query)
    {
        // Test with multiple cultures
        var cultures = new[] { CultureInfo.InvariantCulture, new CultureInfo("en-US"), new CultureInfo("de-DE") };

        var successCount = 0;
        foreach (var culture in cultures)
        {
            var result = this._service.ConvertQuery(query, culture);
            if (result.Success)
            {
                successCount++;
                Assert.NotEmpty(result.FormattedResult);
            }
        }

        // At least one culture should handle it successfully
        Assert.True(successCount > 0, $"No culture could handle: {query}");
    }

    [Theory]
    [InlineData("1 m² to cm²")] // Area
    [InlineData("1 ft² to m²")] // Area
    [InlineData("1 m³ to l")] // Volume
    public void DimensionalUnitsConvertCorrectly(string query)
    {
        // Act
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);

        // Assert
        Assert.True(result.Success, $"Failed to convert: {query}. Error: {result.ErrorMessage}");
        Assert.True(result.Output.Value > 0);
        Assert.NotEmpty(result.FormattedResult);
    }
}

/// <summary>
///     Module verification tests that validate individual modules work as expected
/// </summary>
public class ModuleVerificationTests
{
    [Fact]
    public void LengthModuleWorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetLengthModule();

        // Act & Assert
        Assert.Equal("Length", module.Category);
        Assert.True(module.SupportsSquaredUnits);
        Assert.True(module.SupportsCubicUnits);

        // Test basic conversion
        var success = module.TryConvert(1.0, "m", "ft", out var result);
        Assert.True(success);
        Assert.Equal(3.281, result, 3);

        // Test suggestions
        var suggestions = module.GetCommonConversions(1.0, "m").ToArray();
        Assert.NotEmpty(suggestions);
    }

    [Fact]
    public void TemperatureModuleWorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetTemperatureModule();

        // Act & Assert
        Assert.Equal("Temperature", module.Category);

        // Test basic conversion
        var success = module.TryConvert(0.0, "°C", "°F", out var result);
        Assert.True(success);
        Assert.Equal(32.0, result, 1);

        // Test suggestions
        var suggestions = module.GetCommonConversions(25.0, "°C").ToArray();
        Assert.NotEmpty(suggestions);
    }

    [Fact]
    public void TimeModuleWorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetTimeModule();

        // Act & Assert
        Assert.Equal("Time", module.Category);

        // Test basic conversion
        var success = module.TryConvert(60.0, "min", "h", out var result);
        Assert.True(success);
        Assert.Equal(1.0, result, 2);

        // Test suggestions
        var suggestions = module.GetCommonConversions(60.0, "min").ToArray();
        Assert.NotEmpty(suggestions);
    }

    [Fact]
    public void VolumeModuleWorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetVolumeModule();

        // Act & Assert
        Assert.Equal("Volume", module.Category);

        // Test basic conversion
        var success = module.TryConvert(1000.0, "ml", "l", out var result);
        Assert.True(success);
        Assert.Equal(1.0, result, 3);

        // Test suggestions
        var suggestions = module.GetCommonConversions(1.0, "l").ToArray();
        Assert.NotEmpty(suggestions);
    }

    [Fact]
    public void MassModuleWorksCorrectly()
    {
        // Arrange
        var module = new UnitsNetMassModule();

        // Act & Assert
        Assert.Equal("Mass", module.Category);

        // Test basic conversions
        var success1 = module.TryConvert(1.0, "kg", "lb", out var result1);
        Assert.True(success1);
        Assert.Equal(2.205, result1, 3);

        // Test new units - carats
        var success2 = module.TryConvert(5.0, "ct", "g", out var result2);
        Assert.True(success2);
        Assert.Equal(1.0, result2, 3); // 5 carats = 1 gram

        // Test metric units - dekagram
        var success3 = module.TryConvert(1.0, "dag", "g", out var result3);
        Assert.True(success3);
        Assert.Equal(10.0, result3, 3); // 1 dekagram = 10 grams

        // Test metric tonne
        var success4 = module.TryConvert(1.0, "t", "kg", out var result4);
        Assert.True(success4);
        Assert.Equal(1000.0, result4, 3); // 1 tonne = 1000 kg

        // Test short ton
        var success5 = module.TryConvert(1.0, "short tn", "lb", out var result5);
        Assert.True(success5);
        Assert.Equal(2000.0, result5, 3); // 1 short ton = 2000 pounds

        // Test suggestions
        var suggestions = module.GetCommonConversions(1.0, "kg").ToArray();
        Assert.NotEmpty(suggestions);

        // Test all units are supported
        var supportedUnits = module.GetSupportedUnits();
        var expectedUnits = new[] { "ct", "mg", "cg", "dg", "g", "dag", "hg", "kg", "t", "oz", "lb", "stone", "short tn" };
        foreach (var expectedUnit in expectedUnits)
        {
            Assert.True(supportedUnits.ContainsKey(expectedUnit), $"Missing unit: {expectedUnit}");
        }
    }

    [Theory]
    [InlineData("UnitsNetLengthModule")]
    [InlineData("UnitsNetTemperatureModule")]
    [InlineData("UnitsNetTimeModule")]
    [InlineData("UnitsNetVolumeModule")]
    [InlineData("UnitsNetMassModule")]
    public void AllModulesHaveBasicProperties(string moduleTypeName)
    {
        // Get module type and create instance
        var moduleType = typeof(UnitsNetLengthModule).Assembly.GetTypes()
            .FirstOrDefault(t => t.Name == moduleTypeName);

        Assert.NotNull(moduleType);

        var module = moduleType == typeof(UnitsNetVolumeModule)
            ? Activator.CreateInstance(moduleType, new object?[] { null }) as IConversionModule
            : Activator.CreateInstance(moduleType) as IConversionModule;
        Assert.NotNull(module);

        // Test basic properties
        Assert.NotEmpty(module.Category);
        Assert.NotEmpty(module.GetSupportedUnits());
    }
}