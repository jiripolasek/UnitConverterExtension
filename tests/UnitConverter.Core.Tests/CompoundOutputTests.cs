using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class CompoundOutputTests
{
    [Theory]
    [InlineData("180 cm to ft in", "5 ft 10.9 in")]
    [InlineData("3661 s to h min s", "1 h 1 min 1 s")]
    [InlineData("1 h 30 min to h min", "1 h 30 min")]
    [InlineData("0 s to h min s", "0 h 0 min 0 s")]
    [InlineData("-3661 s to h min s", "-1 h -1 min -1 s")]
    [InlineData("3599.9999 s to h min s", "1 h 0 min 0 s")]
    [InlineData("24 in to ft in", "2 ft 0 in")]
    [InlineData("1.23 m to m cm mm", "1 m 23 cm 0 mm")]
    [InlineData("1 kg to lb oz", "2 lb 3.27 oz")]
    [InlineData("1e-10 s to h min s", "0 h 0 min 1E-10 s")]
    public void CompoundResultsDecomposeAndCarryCorrectly(string query, string expected)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.FormattedResult);
        Assert.True(result.OutputComponents.Length >= 2);
    }

    [Theory]
    [InlineData("1 m to in ft")]
    [InlineData("1 m to ft ft")]
    [InlineData("1 m to ft kg")]
    [InlineData("1 m2 to ft in")]
    [InlineData("10 C to C F")]
    public void InvalidCompoundTargetsAreRejected(string query) =>
        Assert.False(new ConversionService().ConvertQuery(query, CultureInfo.InvariantCulture).Success);

    [Fact]
    public void FrenchCompoundNamesAndDecimalsAreSupported()
    {
        var result = new ConversionService().ConvertQuery("180 centimètres en pieds pouces", CultureInfo.GetCultureInfo("fr-FR"));
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("5 ft 10,9 in", result.FormattedResult);
        Assert.Equal("pied", result.OutputComponents[0].Unit.Metadata!.DisplayName);
    }
}