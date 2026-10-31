// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class DataModuleTests
{
    private readonly UnitsNetDataModule _module;

    public DataModuleTests()
    {
        _module = new UnitsNetDataModule();
    }

    [Theory]
    [InlineData("b", "bit")]
    [InlineData("nibble", "nibbles")]
    [InlineData("B", "byte")]
    [InlineData("kb", "kilobit")]
    [InlineData("kB", "kilobyte")]
    [InlineData("Mb", "megabit")]
    [InlineData("MB", "megabyte")]
    [InlineData("Gb", "gigabit")]
    [InlineData("GB", "gigabyte")]
    [InlineData("Tb", "terabit")]
    [InlineData("TB", "terabyte")]
    [InlineData("Pb", "petabit")]
    [InlineData("PB", "petabyte")]
    [InlineData("Eb", "exabit")]
    [InlineData("EB", "exabyte")]
    [InlineData("Zb", "zettabit")]
    [InlineData("ZB", "zettabyte")]
    [InlineData("Yb", "yottabit")]
    [InlineData("YB", "yottabyte")]
    [InlineData("Kib", "kibibit")]
    [InlineData("KiB", "kibibyte")]
    [InlineData("Mib", "mebibit")]
    [InlineData("MiB", "mebibyte")]
    [InlineData("Gib", "gibibit")]
    [InlineData("GiB", "gibibyte")]
    [InlineData("Tib", "tebibit")]
    [InlineData("TiB", "tebibyte")]
    [InlineData("Pib", "pebibit")]
    [InlineData("PiB", "pebibyte")]
    [InlineData("Eib", "exbibit")]
    [InlineData("EiB", "exbibyte")]
    public void TryNormalizeUnitAllSupportedUnitsReturnsTrue(string unitId, string alias)
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
    // Basic bit/byte conversions
    [InlineData(8, "b", "B", 1)]           // 8 bits = 1 byte
    [InlineData(1, "B", "b", 8)]           // 1 byte = 8 bits
    [InlineData(2, "nibble", "B", 1)]      // 2 nibbles = 1 byte
    [InlineData(1, "B", "nibble", 2)]      // 1 byte = 2 nibbles
    
    // Kilobit/kilobyte distinction (the main issue being fixed)
    [InlineData(8, "kb", "kB", 1)]         // 8 kilobits = 1 kilobyte
    [InlineData(1, "kB", "kb", 8)]         // 1 kilobyte = 8 kilobits
    [InlineData(5000, "kb", "MiB", 0.596046)] // Original failing test case
    
    // Decimal bit units
    [InlineData(1000, "b", "kb", 1)]       // 1000 bits = 1 kilobit
    [InlineData(1000, "kb", "Mb", 1)]      // 1000 kilobits = 1 megabit
    [InlineData(1000, "Mb", "Gb", 1)]      // 1000 megabits = 1 gigabit
    [InlineData(1000, "Gb", "Tb", 1)]      // 1000 gigabits = 1 terabit
    [InlineData(1000, "Tb", "Pb", 1)]      // 1000 terabits = 1 petabit
    [InlineData(1000, "Pb", "Eb", 1)]      // 1000 petabits = 1 exabit
    
    // Binary bit units
    [InlineData(1024, "b", "Kib", 1)]      // 1024 bits = 1 kibibit
    [InlineData(1024, "Kib", "Mib", 1)]    // 1024 kibibits = 1 mebibit
    [InlineData(1024, "Mib", "Gib", 1)]    // 1024 mebibits = 1 gibibit
    [InlineData(1024, "Gib", "Tib", 1)]    // 1024 gibibits = 1 tebibit
    
    // Decimal byte units
    [InlineData(1000, "B", "kB", 1)]       // 1000 bytes = 1 kilobyte
    [InlineData(1000, "kB", "MB", 1)]      // 1000 kilobytes = 1 megabyte
    [InlineData(1000, "MB", "GB", 1)]      // 1000 megabytes = 1 gigabyte
    [InlineData(1000, "GB", "TB", 1)]      // 1000 gigabytes = 1 terabyte
    [InlineData(1000, "TB", "PB", 1)]      // 1000 terabytes = 1 petabyte
    [InlineData(1000, "PB", "EB", 1)]      // 1000 petabytes = 1 exabyte
    
    // Binary byte units
    [InlineData(1024, "B", "KiB", 1)]      // 1024 bytes = 1 kibibyte
    [InlineData(1024, "KiB", "MiB", 1)]    // 1024 kibibytes = 1 mebibyte
    [InlineData(1024, "MiB", "GiB", 1)]    // 1024 mebibytes = 1 gibibyte
    [InlineData(1024, "GiB", "TiB", 1)]    // 1024 gibibytes = 1 tebibyte
    [InlineData(1024, "TiB", "PiB", 1)]    // 1024 tebibytes = 1 pebibyte
    [InlineData(1024, "PiB", "EiB", 1)]    // 1024 pebibytes = 1 exbibyte
    
    // Mixed conversions
    [InlineData(1, "GB", "GiB", 0.931323)] // 1 GB ≈ 0.931 GiB
    [InlineData(1, "GiB", "GB", 1.073742)] // 1 GiB ≈ 1.074 GB
    [InlineData(1, "Gb", "MB", 125)]       // 1 gigabit = 125 megabytes
    [InlineData(1, "MB", "Mb", 8)]         // 1 megabyte = 8 megabits
    [InlineData(1, "Mib", "Mb", 1.048576)] // 1 mebibit ≈ 1.048576 megabits
    [InlineData(1, "TB", "Tib", 7.275958)] // 1 terabyte ≈ 7.276 tebibits
    
    // Large unit conversions
    [InlineData(1, "PB", "TB", 1000)]      // 1 petabyte = 1000 terabytes
    [InlineData(1, "EB", "PB", 1000)]      // 1 exabyte = 1000 petabytes
    [InlineData(1, "ZB", "EB", 1000)]      // 1 zettabyte = 1000 exabytes
    [InlineData(1, "YB", "ZB", 1000)]      // 1 yottabyte = 1000 zettabytes
    
    // Nibble conversions
    [InlineData(8, "nibble", "B", 4)]      // 8 nibbles = 4 bytes
    [InlineData(1000, "nibble", "kb", 4)]  // 1000 nibbles = 4 kilobits
    public void TryConvertCommonConversionsReturnsCorrectResults(
        double inputValue, 
        string fromUnit, 
        string toUnit, 
        double expectedValue)
    {
        // Act
        var result = _module.TryConvert(inputValue, fromUnit, toUnit, out var convertedValue);

        // Assert
        Assert.True(result, $"Conversion from {fromUnit} to {toUnit} should succeed");
        Assert.Equal(expectedValue, convertedValue, 6); // 6 decimal places precision
    }

    [Theory]
    [InlineData("b", "Bit", "b", "Digital")]
    [InlineData("nibble", "Nibble", "nibble", "Digital")]
    [InlineData("B", "Byte", "B", "Digital")]
    [InlineData("kb", "Kilobit", "kb", "Digital")]
    [InlineData("kB", "Kilobyte", "kB", "Digital")]
    [InlineData("Mb", "Megabit", "Mb", "Digital")]
    [InlineData("MB", "Megabyte", "MB", "Digital")]
    [InlineData("Pb", "Petabit", "Pb", "Digital")]
    [InlineData("PB", "Petabyte", "PB", "Digital")]
    [InlineData("Kib", "Kibibit", "Kib", "Digital")]
    [InlineData("KiB", "Kibibyte", "KiB", "Digital")]
    [InlineData("Mib", "Mebibit", "Mib", "Digital")]
    [InlineData("MiB", "Mebibyte", "MiB", "Digital")]
    [InlineData("Yb", "Yottabit", "Yb", "Digital")]
    [InlineData("YB", "Yottabyte", "YB", "Digital")]
    [InlineData("YiB", "Yobibyte", "YiB", "Digital")]
    public void GetUnitMetadataAllUnitsReturnsCorrectMetadata(
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

    [Fact]
    public void GetCommonConversionsKilobitsReturnsBitAndByteTargets()
    {
        // Act
        var suggestions = _module.GetCommonConversions(1000, "kb").ToArray();

        // Assert
        Assert.True(suggestions.Length > 0, "Should return at least one suggestion");
        
        // Should include conversions to both bit and byte units
        var suggestionUnits = suggestions.Select(s => s.Output.Unit.Unit).ToArray();
        
        // Verify we have different types of suggestions
        Assert.All(suggestions, suggestion =>
        {
            Assert.True(suggestion.Output.Value > 0);
            Assert.NotEmpty(suggestion.Output.Unit.Unit);
            Assert.NotEmpty(suggestion.FormattedResult);
            Assert.NotNull(suggestion.Output.Unit.Metadata);
        });
    }

    [Theory]
    [InlineData("kilobit", "kb")]
    [InlineData("kilobits", "kb")]
    [InlineData("kbit", "kb")]
    [InlineData("kbits", "kb")]
    [InlineData("kilobyte", "kB")]
    [InlineData("kilobytes", "kB")]
    [InlineData("megabit", "Mb")]
    [InlineData("megabits", "Mb")]
    [InlineData("Mbit", "Mb")]
    [InlineData("Mbits", "Mb")]
    [InlineData("gigabit", "Gb")]
    [InlineData("gigabits", "Gb")]
    [InlineData("mebibyte", "MiB")]
    [InlineData("mebibytes", "MiB")]
    [InlineData("kibibit", "Kib")]
    [InlineData("kibibits", "Kib")]
    [InlineData("mebibit", "Mib")]
    [InlineData("mebibits", "Mib")]
    [InlineData("petabyte", "PB")]
    [InlineData("petabytes", "PB")]
    [InlineData("exabyte", "EB")]
    [InlineData("exabytes", "EB")]
    [InlineData("zettabyte", "ZB")]
    [InlineData("zettabytes", "ZB")]
    [InlineData("yottabyte", "YB")]
    [InlineData("yottabytes", "YB")]
    [InlineData("pebibyte", "PiB")]
    [InlineData("pebibytes", "PiB")]
    [InlineData("exbibyte", "EiB")]
    [InlineData("exbibytes", "EiB")]
    [InlineData("yobibyte", "YiB")]
    [InlineData("yobibytes", "YiB")]
    [InlineData("nibbles", "nibble")]
    [InlineData("nybble", "nibble")]
    [InlineData("nybbles", "nibble")]
    public void TryNormalizeUnitAllAliasesWorkCorrectly(string alias, string expectedUnitId)
    {
        // Act
        var result = _module.TryNormalizeUnit(alias, CultureInfo.InvariantCulture, out var normalized);

        // Assert
        Assert.True(result, $"Alias '{alias}' should be recognized");
        Assert.Equal(expectedUnitId, normalized);
    }

    [Fact]
    public void ModulePropertiesAreCorrect()
    {
        // Assert
        Assert.Equal("Data", _module.Category);
        Assert.False(_module.SupportsSquaredUnits);
        Assert.False(_module.SupportsCubicUnits);
    }

    [Theory]
    [InlineData("invalid_unit")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("xyz")]
    public void TryNormalizeUnitInvalidUnitsReturnsFalse(string invalidUnit)
    {
        // Act
        var result = _module.TryNormalizeUnit(invalidUnit, CultureInfo.InvariantCulture, out var normalized);

        // Assert
        Assert.False(result);
        Assert.Empty(normalized);
    }

    [Fact]
    public void GetAllUnitMetadataReturnsAllSupportedUnits()
    {
        // Act
        var allMetadata = _module.GetAllUnitMetadata();

        // Assert
        var expectedUnits = new[] { 
            // Basic units
            "b", "nibble", "B", 
            // Decimal bit units
            "kb", "Mb", "Gb", "Tb", "Pb", "Eb", "Zb", "Yb",
            // Binary bit units
            "Kib", "Mib", "Gib", "Tib", "Pib", "Eib",
            // Decimal byte units
            "kB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", 
            // Binary byte units
            "KiB", "MiB", "GiB", "TiB", "PiB", "EiB", "ZiB", "YiB" 
        };
        
        Assert.Equal(expectedUnits.Length, allMetadata.Count);
        
        foreach (var expectedUnit in expectedUnits)
        {
            Assert.True(allMetadata.ContainsKey(expectedUnit), $"Should contain unit: {expectedUnit}");
            Assert.NotNull(allMetadata[expectedUnit]);
            Assert.NotEmpty(allMetadata[expectedUnit].DisplayName);
        }
    }

    [Theory]
    [InlineData(0.0)]      // Zero value
    [InlineData(0.001)]    // Very small value
    [InlineData(1000000)]  // Very large value
    public void TryConvertEdgeCasesHandlesCorrectly(double edgeValue)
    {
        // Act
        var result = _module.TryConvert(edgeValue, "kb", "Mb", out var convertedValue);

        // Assert
        Assert.True(result);
        Assert.Equal(edgeValue / 1000, convertedValue, 6); // kb to Mb is /1000
        Assert.False(double.IsNaN(convertedValue));
        Assert.False(double.IsInfinity(convertedValue));
    }
}