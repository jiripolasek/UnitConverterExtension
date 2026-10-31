using System.Globalization;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class DataRateTests
{
    private readonly ConversionService _service = new();

    [Theory]
    [InlineData("100 Mbps to MB/s", 12.5)]
    [InlineData("100 MBps to Mb/s", 800)]
    [InlineData("1 MB/s to b/s", 8000000)]
    [InlineData("1 MiB/s to Mib/s", 8)]
    [InlineData("1 Mib/s to Mb/s", 1.048576)]
    [InlineData("1 MiB/s to MB/s", 1.048576)]
    [InlineData("1 Gb/s to MB/s", 125)]
    [InlineData("1 kB/s to kb/s", 8)]
    [InlineData("1 TiB/s to GiB/s", 1024)]
    [InlineData("1 TB/s to GB/s", 1000)]
    [InlineData("1 megabyte per second to megabits per second", 8)]
    [InlineData("0 Mbps to MBps", 0)]
    public void ConvertsRatesWithoutConfusingBitsBytesOrPrefixes(string query, double expected)
    {
        var result = _service.ConvertQuery(query, CultureInfo.InvariantCulture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(expected, result.Output.Value, 8);
        Assert.Equal(UnitType.DataRate, result.Output.Unit.Type);
    }

    [Theory]
    [InlineData("1 mbps to MB/s")]
    [InlineData("1 MB/s to MB")]
    [InlineData("1 Mb/s² to MB/s²")]
    [InlineData("1e100 Mb/s to b/s")]
    public void RejectsUnknownOrIncompatibleRates(string query)
    {
        Assert.False(_service.ConvertQuery(query, CultureInfo.InvariantCulture).Success);
    }

    [Fact]
    public void FrenchNamesSuggestionsAndCompletionsUseRateUnits()
    {
        var french = CultureInfo.GetCultureInfo("fr-FR");
        var result = _service.ConvertQuery("1,5 mégaoctets par seconde en mégabits par seconde", french);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(12, result.Output.Value);
        Assert.Equal("mégabit par seconde", result.Output.Unit.Metadata!.DisplayName);

        var implicitResult = _service.ConvertQuery("100 Mbps", french);
        Assert.Contains(implicitResult.Suggestions, item => item.Output.Unit.Unit == "MB/s" && item.Output.Value == 12.5);
        var incomplete = _service.ConvertQuery("100 Mbps en", french);
        Assert.NotEmpty(incomplete.Completions);
        Assert.All(incomplete.Completions, item => Assert.Equal(UnitType.DataRate, item.Unit.Type));
        Assert.Contains(_service.ConvertQuery("100 Mbp", french).Completions, item => item.Query == "100 Mb/s");
    }
}