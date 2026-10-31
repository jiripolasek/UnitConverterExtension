using System.Collections;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.UnitTest;

public class LocalizationCoverageTests
{
    private static readonly string[] LocaleNames =
    [
        "en-US", "cs-CZ", "de-DE", "es-ES", "fr-FR", "it-IT", "ja-JP", "ko-KR",
        "nl-NL", "pl-PL", "pt-BR", "pt-PT", "zh-CN", "zh-TW"
    ];

    public static IEnumerable<object[]> Cultures => LocaleNames.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Cultures))]
    public void EveryLocaleHasCompleteResourcesAndPreservesFormatArguments(string name)
    {
        var manager = new ResourceManager("JPSoftworks.UnitConverter.Core.Resources.Strings", typeof(ConverterText).Assembly);
        var neutral = manager.GetResourceSet(CultureInfo.InvariantCulture, true, false)!;
        var culture = CultureInfo.GetCultureInfo(name == "fr-FR" ? "fr" : name == "en-US" ? "" : name);
        var localized = manager.GetResourceSet(culture, true, false);
        Assert.NotNull(localized);
        var expected = neutral.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();
        Assert.Equal(expected, localized.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order());
        foreach (var key in expected.Where(key => key != "UnitNames"))
        {
            var value = localized.GetString(key);
            Assert.False(string.IsNullOrWhiteSpace(value), $"{name}: {key}");
            Assert.Equal(Arguments(key), Arguments(value!));
        }
    }

    [Theory]
    [MemberData(nameof(Cultures))]
    public void TranslatedUnitNamesResolveAndConvertWithoutChangingTheirMeaning(string name)
    {
        if (name == "en-US")
            return;
        var culture = CultureInfo.GetCultureInfo(name);
        var service = new ConversionService();
        foreach (var module in service.Modules)
        {
            foreach (var unit in module.GetSupportedUnits())
            {
                var names = ConverterText.GetUnitNames(module.Category, unit.Key, culture);
                Assert.NotEmpty(names);
                Assert.Equal(names[0], module.GetUnitMetadata(unit.Key, culture)!.DisplayName);
                foreach (var alias in names)
                {
                    Assert.True(module.TryNormalizeUnit(alias.ToUpper(culture), culture, out var id), $"{name}: {alias}");
                    Assert.Equal(unit.Key, id);
                    var converted = service.ConvertQuery($"1 {alias} -> {unit.Key}", culture);
                    Assert.True(converted.Success, $"{name}: {alias}: {converted.ErrorMessage}");
                    Assert.Equal(1, converted.Output.Value, 8);
                }
            }
        }
    }

    [Theory]
    [InlineData("cs-CZ", "1 v v mV", 1000)]
    [InlineData("pl-PL", "1 W w kW", 0.001)]
    [InlineData("es-ES", "1 A a mA", 1000)]
    [InlineData("de-DE", "12 in in cm", 30.48)]
    public void NativeConnectorWordsCanOverlapUnitSymbols(string name, string query, double value)
    {
        var result = new ConversionService().ConvertQuery(query, CultureInfo.GetCultureInfo(name));
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(value, result.Output.Value, 8);
    }

    [Fact]
    public void LocalizedNamesDoNotReplaceExactUnitIdentifiers()
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        var service = new ConversionService();
        var gradian = service.ConvertQuery("1 grad in deg", culture);
        var degree = service.ConvertQuery("1 Grad in deg", culture);
        Assert.True(gradian.Success, gradian.ErrorMessage);
        Assert.True(degree.Success, degree.ErrorMessage);
        Assert.Equal(0.9, gradian.Output.Value, 8);
        Assert.Equal(1, degree.Output.Value, 8);
        var decagram = service.ConvertQuery("1 dag -> g", CultureInfo.GetCultureInfo("nl-NL"));
        Assert.True(decagram.Success, decagram.ErrorMessage);
        Assert.Equal(10, decagram.Output.Value, 8);
        var polish = CultureInfo.GetCultureInfo("pl-PL");
        var calorie = service.ConvertQuery("1 cal -> J", polish);
        var inch = service.ConvertQuery("1 cal międzynarodowy -> cm", polish);
        Assert.True(calorie.Success, calorie.ErrorMessage);
        Assert.True(inch.Success, inch.ErrorMessage);
        Assert.Equal(4.184, calorie.Output.Value, 8);
        Assert.Equal(2.54, inch.Output.Value, 8);
    }

    [Fact]
    public void ManifestResourcesMatchTheSupportedLocalesAndRuntimeNames()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "UnitConverterExtension.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        var packageRoot = Path.Combine(root.FullName, "src", "UnitConverterExtension.Package");
        var strings = Path.Combine(packageRoot, "Strings");
        Assert.Equal(LocaleNames.Order(), Directory.GetDirectories(strings).Select(Path.GetFileName).Order());
        var manifest = XDocument.Load(Path.Combine(packageRoot, "Package.appxmanifest"));
        var references = manifest.Descendants().Attributes()
            .Where(attribute => attribute.Name.LocalName is "DisplayName" or "Description").Select(attribute => attribute.Value)
            .Append(manifest.Root!.Elements().Single(element => element.Name.LocalName == "Properties")
                .Elements().Single(element => element.Name.LocalName == "DisplayName").Value).ToArray();
        Assert.All(references, reference => Assert.StartsWith("ms-resource:", reference));
        foreach (var name in LocaleNames)
        {
            var values = XDocument.Load(Path.Combine(strings, name, "Resources.resw")).Root!.Elements("data")
                .ToDictionary(element => element.Attribute("name")!.Value, element => element.Element("value")!.Value);
            Assert.All(references, reference => Assert.False(string.IsNullOrWhiteSpace(values[reference[12..]])));
            Assert.Equal(ConverterText.Get("Unit Converter for Command Palette", CultureInfo.GetCultureInfo(name)), values["AppDisplayName"]);
        }
    }

    private static string[] Arguments(string text) => Regex.Matches(text, @"\{\d+\}").Cast<Match>().Select(match => match.Value).Order().ToArray();
}