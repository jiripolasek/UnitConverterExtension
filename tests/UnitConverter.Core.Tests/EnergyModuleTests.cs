// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class EnergyModuleTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 kWh to J", 3600000, "J", 0)]
    [InlineData("500 cal to kJ", 2.092, "kJ", 3)]
    [InlineData("1 BTU to kJ", 1.05506, "kJ", 5)]
    [InlineData("1 eV to J", 1.602176634e-19, "J", 15)]
    [InlineData("10 ft·lb to J", 13.5581794833, "J", 6)]
    public void EnergyConversions(string query, double expectedValue, string expectedUnit, int precision)
    {
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success);
        Assert.Equal(expectedUnit, result.Output.Unit.Unit);
        Assert.Equal(expectedValue, result.Output.Value, precision);
    }
}
