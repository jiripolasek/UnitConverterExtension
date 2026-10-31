// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class PressureModuleTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("1 atm to kPa", 101.325, "kPa", 3)]
    [InlineData("30 psi to bar", 2.068, "bar", 3)]
    [InlineData("760 mmHg to atm", 1.0, "atm", 6)]
    public void PressureConversions(string query, double expectedValue, string expectedUnit, int precision)
    {
        var result = this._service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success);
        Assert.Equal(expectedUnit, result.Output.Unit.Unit);
        Assert.Equal(expectedValue, result.Output.Value, precision);
    }
}
