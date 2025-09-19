// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class DataModuleScenarioTests
{
    private readonly UnitsNetDataModule _module = new();

    [Theory]
    [InlineData(8, "kb", "B", 1000.0)]
    [InlineData(5000, "kb", "MiB", 0.596046)]
    [InlineData(1, "Mb", "kb", 1000.0)]
    [InlineData(1024, "Kib", "Mib", 1.0)]
    public void Data_CoreScenarios(double value, string from, string to, double expected)
    {
        var ok = _module.TryConvert(value, from, to, out var result);
        Assert.True(ok);
        Assert.Equal(expected, result, 6);
    }
}
