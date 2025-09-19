// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class SpeedModuleTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("100 km/h to mph", 62.137, "mph", 3)]
    [InlineData("30 m/s to ft/s", 98.4252, "ft/s", 4)]
    [InlineData("10 kn to km/h", 18.52, "km/h", 2)]
    public void Speed_Conversions(string query, double expectedValue, string expectedUnit, int precision)
    {
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success);
        Assert.Equal(expectedUnit, result.Output.Unit.Unit);
        Assert.Equal(expectedValue, result.Output.Value, precision);
    }
}
