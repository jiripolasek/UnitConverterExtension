using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class ConversionConnectorTests
{
    [Theory]
    [InlineData("to")]
    [InlineData("as")]
    [InlineData("en")]
    [InlineData("vers")]
    [InlineData("in")]
    public void ContextWordsCanBeSourceTargetAndMixedUnits(string word)
    {
        IConversionModule[] modules = [new ContextUnitModule(word, "m", "cm")];
        var implicitQuery = Parse($"1 {word}", modules);
        Assert.True(implicitQuery.IsImplicitConversion);
        Assert.Equal(word, implicitQuery.FromUnit);

        var source = Parse($"1 {word} to m", modules);
        Assert.False(source.IsImplicitConversion);
        Assert.Equal(word, source.FromUnit);
        Assert.Equal("m", source.ToUnit);

        var target = Parse($"1 m to {word}", modules);
        Assert.False(target.IsImplicitConversion);
        Assert.Equal("m", target.FromUnit);
        Assert.Equal(word, target.ToUnit);

        var mixed = Parse($"1 m 2 {word} in cm", modules);
        Assert.Equal(["m", word], mixed.Components.Select(component => component.Unit.Unit).ToArray());
        Assert.Equal("cm", mixed.ToUnit);

        var squared = Parse($"1 {word}2 to m2", modules);
        Assert.Equal(word, squared.FromUnit);
        Assert.Equal(UnitDimension.Squared, squared.Dimension);
    }

    [Theory]
    [InlineData("to")]
    [InlineData("as")]
    [InlineData("en")]
    [InlineData("vers")]
    [InlineData("in")]
    public void ContextWordsInsideMultiwordUnitsArePreserved(string word)
    {
        var sourceUnit = $"source {word} unit";
        var targetUnit = $"target {word} unit";
        IConversionModule[] modules = [new ContextUnitModule(sourceUnit, targetUnit, "m")];
        var implicitQuery = Parse($"1 {sourceUnit}", modules);
        Assert.True(implicitQuery.IsImplicitConversion);
        Assert.Equal(sourceUnit, implicitQuery.FromUnit);

        var explicitQuery = Parse($"1 {sourceUnit} to {targetUnit}", modules);
        Assert.False(explicitQuery.IsImplicitConversion);
        Assert.Equal(sourceUnit, explicitQuery.FromUnit);
        Assert.Equal(targetUnit, explicitQuery.ToUnit);

        var mixed = Parse($"1 m 2 {sourceUnit} as {targetUnit}", modules);
        Assert.Equal(["m", sourceUnit], mixed.Components.Select(component => component.Unit.Unit).ToArray());
        Assert.Equal(targetUnit, mixed.ToUnit);
    }

    [Fact]
    public void ACompleteUnitWinsAndSymbolsCanForceAConversion()
    {
        IConversionModule[] modules = [new ContextUnitModule("a", "b", "a in b")];
        var implicitQuery = Parse("1 a in b", modules);
        Assert.True(implicitQuery.IsImplicitConversion);
        Assert.Equal("a in b", implicitQuery.FromUnit);

        var explicitQuery = Parse("1a->b", modules);
        Assert.False(explicitQuery.IsImplicitConversion);
        Assert.Equal("a", explicitQuery.FromUnit);
        Assert.Equal("b", explicitQuery.ToUnit);
    }

    [Fact]
    public void TheLongestRecognizedSourceWinsBetweenValidSplits()
    {
        IConversionModule[] modules = [new ContextUnitModule("a", "a in b", "b in c", "c")];
        var parsed = Parse("1 a in b in c", modules);
        Assert.False(parsed.IsImplicitConversion);
        Assert.Equal("a in b", parsed.FromUnit);
        Assert.Equal("c", parsed.ToUnit);
    }

    [Fact]
    public void ConnectorCasingIsIndependentOfCurrentCultureAndPreservesUnitCasing()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            IConversionModule[] modules = [new ContextUnitModule("A", "a")];
            var parsed = Parse("1 A IN a", modules);
            Assert.False(parsed.IsImplicitConversion);
            Assert.Equal("A", parsed.FromUnit);
            Assert.Equal("a", parsed.ToUnit);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("1 min", false)]
    [InlineData("1 stone", false)]
    [InlineData("1 m inft", false)]
    [InlineData("1 m in/ft", false)]
    [InlineData("1 in cm", false)]
    [InlineData("1 m in ft", true)]
    [InlineData("1m->ft", true)]
    public void WordsNeedSeparateTokensAndACompleteSource(string query, bool expected)
    {
        var (indicator, _) = ConversionQueryParser.FindConversionBoundary(query, CultureInfo.InvariantCulture, new ConversionService().Modules);
        Assert.Equal(expected, indicator.Success);
    }

    private static ParsedConversionQuery Parse(string query, IEnumerable<IConversionModule> modules)
    {
        var parsed = ConversionQueryParser.TryParseQuery(query, CultureInfo.GetCultureInfo("fr-FR"), modules, ConversionWordLanguage.InputLanguageAndEnglish);
        Assert.NotNull(parsed);
        return parsed;
    }

    private sealed class ContextUnitModule(params string[] units) : DescriptorBasedConversionModule(
        units.Select(unit => new UnitDescriptor { Id = unit }))
    {
        public override string Category => "Test";
        public override bool SupportsSquaredUnits => true;
        public override bool SupportsCubicUnits => true;
        public override bool IsCaseSensitive => true;
        protected override UnitType GetUnitType() => UnitType.Length;
        public override bool TryConvert(double value, string fromUnit, string toUnit, out double result) => throw new NotSupportedException();
    }
}