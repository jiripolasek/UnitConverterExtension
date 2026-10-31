using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class CjkQueryTests
{
    private readonly ConversionService _service = new();
    private static readonly string[] LocaleNames = ["en-US", "fr-FR", "ja-JP", "ko-KR", "zh-CN", "zh-TW"];

    private static readonly (string Culture, string Query, string Unit, double Value)[] Examples =
    [
        ("ja-JP", "1メートルをフィートに変換", "ft", 1 / 0.3048),
        ("ja-JP", "1 メートル を フィート に 変換", "ft", 1 / 0.3048),
        ("ja-JP", "1 m を ft に", "ft", 1 / 0.3048),
        ("ja-JP", "1メートルからフィートへ換算する", "ft", 1 / 0.3048),
        ("ja-JP", "(2 + 3) m を cm に換算", "cm", 500),
        ("ja-JP", "-1 m を cm に変換", "cm", -100),
        ("ko-KR", "1미터를 피트로 변환", "ft", 1 / 0.3048),
        ("ko-KR", "1 미터 를 피트 로 변환", "ft", 1 / 0.3048),
        ("ko-KR", "1 m 를 ft 로", "ft", 1 / 0.3048),
        ("ko-KR", "1미터에서 피트로 환산하기", "ft", 1 / 0.3048),
        ("ko-KR", "1그램을 킬로그램으로 환산", "kg", 0.001),
        ("ko-KR", "-1 m 를 cm 로 변환", "cm", -100),
        ("zh-CN", "1米转换为英尺", "ft", 1 / 0.3048),
        ("zh-CN", "1 米 换算成 英尺", "ft", 1 / 0.3048),
        ("zh-CN", "1 m 转 ft", "ft", 1 / 0.3048),
        ("zh-CN", "1米转换成英尺", "ft", 1 / 0.3048),
        ("zh-CN", "1米换算为英尺", "ft", 1 / 0.3048),
        ("zh-CN", "1米到英尺", "ft", 1 / 0.3048),
        ("zh-CN", "-1米转厘米", "cm", -100),
        ("zh-TW", "1公尺換算成英尺", "ft", 1 / 0.3048),
        ("zh-TW", "1 公尺 轉換為 英呎", "ft", 1 / 0.3048),
        ("zh-TW", "1 m 轉 ft", "ft", 1 / 0.3048),
        ("zh-TW", "1公尺轉換成英呎", "ft", 1 / 0.3048),
        ("zh-TW", "1公尺換算為英呎", "ft", 1 / 0.3048),
        ("zh-TW", "1公尺到英呎", "ft", 1 / 0.3048),
        ("zh-TW", "-1公尺轉公分", "cm", -100)
    ];

    public static IEnumerable<object[]> NaturalQueries =>
        from example in (from item in Examples
                        from query in new[] { item.Query, item.Query.Replace(" ", "") }
                        select (item.Culture, Query: query, item.Unit, item.Value)).Distinct()
        from mode in Enum.GetValues<ConversionWordLanguage>()
        select new object[] { example.Culture, example.Query, mode, example.Unit, example.Value };

    [Theory]
    [InlineData("ja-JP", "1mをftに")]
    [InlineData("ko-KR", "1m를ft로")]
    [InlineData("zh-CN", "1m转ft")]
    [InlineData("zh-TW", "1m轉ft")]
    public void PublicParserPreservesTheOriginalQueryAndUnitSpans(string name, string query)
    {
        var parsed = ConversionQueryParser.TryParseQuery(query, CultureInfo.GetCultureInfo(name));
        Assert.NotNull(parsed);
        Assert.False(parsed.IsImplicitConversion);
        Assert.Equal(query, parsed.OriginalQuery);
        Assert.Equal(1, parsed.Value);
        Assert.Equal("m", parsed.FromUnit);
        Assert.Equal("ft", parsed.ToUnit);
    }

    [Theory]
    [InlineData("1 m", "ft", "1 m를 ft로")]
    [InlineData("1 kg", "g", "1 kg을 g으로")]
    [InlineData("1 N", "Pa", "1 N을 Pa로")]
    [InlineData("1그램", "킬로그램", "1그램을 킬로그램으로")]
    [InlineData("1 g/cm³", "kg/m³", "1 g/cm³를 kg/m³로")]
    [InlineData("1 gal", "l", "1 gal을 l로")]
    [InlineData("1 Ω", "kohm", "1 Ω을 kohm으로")]
    [InlineData("5'11\"", "cm", "5'11\"를 cm로")]
    [InlineData("180 cm", "ft in", "180 cm를 ft in로")]
    public void KoreanExampleParticlesFollowThePronouncedUnitEnding(string source, string target, string expected) =>
        Assert.Equal(expected, _service.FormatConversionQuery(source, target, CultureInfo.GetCultureInfo("ko-KR")));

    [Theory]
    [MemberData(nameof(NaturalQueries))]
    public void NaturalQueriesConvertWithAndWithoutSpaces(string culture, string query, ConversionWordLanguage mode, string unit, double value)
    {
        var result = _service.ConvertQuery(query, CultureInfo.GetCultureInfo(culture), mode);
        Assert.True(result.Success, $"{query}: {result.ErrorMessage}");
        Assert.Equal(unit, result.Output.Unit.Unit);
        Assert.Equal(value, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("ja-JP", "1 m に ft")]
    [InlineData("ja-JP", "1 m を ft")]
    [InlineData("ko-KR", "1 m 에서 ft")]
    [InlineData("ko-KR", "1 m 을 ft")]
    [InlineData("ko-KR", "1 m 를 ft")]
    [InlineData("zh-CN", "1 m 转为 ft")]
    [InlineData("zh-TW", "1 m 轉為 ft")]
    public void PreviouslySupportedSpacedWordsRemainAccepted(string name, string query)
    {
        var result = _service.ConvertQuery(query, CultureInfo.GetCultureInfo(name));
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal("ft", result.Output.Unit.Unit);
        Assert.Equal(1 / 0.3048, result.Output.Value, 8);
    }

    [Theory]
    [InlineData("ja-JP", "1mをftに")]
    [InlineData("ko-KR", "1m를ft로")]
    [InlineData("zh-CN", "1m转ft")]
    [InlineData("zh-TW", "1m轉ft")]
    public void NaturalGrammarDoesNotEnableOtherLanguagesOrUnboundedEnglishWords(string name, string query)
    {
        foreach (var mode in Enum.GetValues<ConversionWordLanguage>())
        {
            foreach (var other in LocaleNames.Where(other => other != name))
            {
                Assert.False(_service.ConvertQuery(query, CultureInfo.GetCultureInfo(other), mode).Success, other);
            }

            var culture = CultureInfo.GetCultureInfo(name);
            foreach (var word in new[] { "to", "as", "in" })
            {
                Assert.Equal(mode != ConversionWordLanguage.InputLanguageOnly, _service.ConvertQuery($"1 m {word} ft", culture, mode).Success);
            }

            Assert.False(_service.ConvertQuery("1mtoft", culture, mode).Success);
            Assert.True(_service.ConvertQuery("1m->ft", culture, mode).Success);
        }
    }

    [Theory]
    [InlineData("ja-JP", "1mを", "1mをcmに")]
    [InlineData("ja-JP", "1mをcに換算", "1mをcmに")]
    [InlineData("ko-KR", "1m를", "1m를 cm로")]
    [InlineData("ko-KR", "1m를c로변환", "1m를 cm로")]
    [InlineData("zh-CN", "1m转", "1m转cm")]
    [InlineData("zh-CN", "1m换算成厘", "1m转cm")]
    [InlineData("zh-TW", "1m轉", "1m轉cm")]
    [InlineData("zh-TW", "1m換算成公", "1m轉cm")]
    public void TargetCompletionsUseNaturalGrammarAndCanBeExecuted(string name, string query, string expected)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        var result = _service.ConvertQuery(query, culture);
        Assert.False(result.Success);
        Assert.True(result.IsIncomplete, result.ErrorMessage);
        Assert.Contains(result.Completions, completion => completion.Query == expected);
        Assert.All(result.Completions, completion =>
        {
            var converted = _service.ConvertQuery(completion.Query, culture);
            Assert.True(converted.Success, completion.Query);
            Assert.Equal(converted.FormattedResult, completion.FormattedResult);
        });
    }

    [Theory]
    [InlineData("ja-JP", "を", "に")]
    [InlineData("ko-KR", "를", "로")]
    [InlineData("zh-CN", "转", "")]
    [InlineData("zh-TW", "轉", "")]
    public void NativeBoundariesPreserveMixedMeasuresMultiwordUnitsAndSymbolCase(string name, string connector, string suffix)
    {
        var culture = CultureInfo.GetCultureInfo(name);
        foreach (var (source, target) in new[] { ("1 ft 2 in", "cm"), ("1 in Hg", "Pa"), ("1 lbf in", "Nm"),
                     ("1 Nm", "lbf in"), ("180 cm", "ft in"), ("1 L/100km", "mpg US"), ("1 mm", "Mm") })
        {
            var expected = _service.ConvertQuery($"{source}->{target}", culture);
            var actual = _service.ConvertQuery($"{source}{connector}{target}{suffix}", culture);
            Assert.True(expected.Success, expected.ErrorMessage);
            Assert.True(actual.Success, actual.ErrorMessage);
            Assert.Equal(expected.Output, actual.Output);
            Assert.Equal(expected.OutputComponents, actual.OutputComponents);
        }
        foreach (var query in new[] { "12 in", "1 in Hg", "1 lbf in", "1 ft 2 in" })
        {
            Assert.True(_service.ConvertQuery(query, culture).Success, query);
        }

        var result = _service.ConvertQuery($"10 mN{connector}N{suffix}", culture);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(0.01, result.Output.Value, 8);
        var alternative = Assert.Single(result.OtherInterpretations, item => item.Query == $"10 MN{connector}N{suffix}");
        Assert.True(_service.ConvertQuery(alternative.Query, culture).Success);
        var ambiguous = _service.ConvertQuery($"10 mn{connector}N{suffix}", culture);
        Assert.False(ambiguous.Success);
        Assert.True(ambiguous.IsIncomplete);
        Assert.NotEmpty(ambiguous.OtherInterpretations);
    }

    [Theory]
    [InlineData("ja-JP", "1mをftに余分")]
    [InlineData("ko-KR", "1m를ft로garbage")]
    [InlineData("zh-CN", "1m转ft垃圾")]
    [InlineData("zh-TW", "1m轉ft垃圾")]
    [InlineData("ja-JP", "1mをkgに")]
    [InlineData("ko-KR", "1m를kg으로")]
    public void InvalidTargetsAndTrailingTextAreNotSilentlyDiscarded(string name, string query) =>
        Assert.False(_service.ConvertQuery(query, CultureInfo.GetCultureInfo(name)).Success);

    [Fact]
    public void KnownUnitSpellingsWinOverParticlesAndTheLongestKnownSourceWins()
    {
        IConversionModule[] modules = [new UnitsNetLengthModule(), new ParticleUnitModule()];
        var culture = CultureInfo.GetCultureInfo("ja-JP");
        var whole = ConversionQueryParser.TryParseQuery("1 mをftに", culture, modules)!;
        Assert.True(whole.IsImplicitConversion);
        Assert.Equal("mをftに", whole.FromUnit);
        var source = ConversionQueryParser.TryParseQuery("1 mをftにをcmに", culture, modules)!;
        Assert.Equal("mをftに", source.FromUnit);
        Assert.Equal("cm", source.ToUnit);
        var target = ConversionQueryParser.TryParseQuery("1 cmをftに", culture, modules)!;
        Assert.Equal("ftに", target.ToUnit);
    }

    private sealed class ParticleUnitModule : DescriptorBasedConversionModule
    {
        public ParticleUnitModule() : base([new UnitDescriptor { Id = "mをftに" }, new UnitDescriptor { Id = "ftに" }]) { }
        public override string Category => "Length";
        public override bool SupportsSquaredUnits => false;
        public override bool SupportsCubicUnits => false;
        protected override UnitType GetUnitType() => UnitType.Length;
        public override bool TryConvert(double value, string fromUnit, string toUnit, out double result) { result = value; return true; }
    }
}
