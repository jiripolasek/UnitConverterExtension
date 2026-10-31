using System.Globalization;
using System.Text.RegularExpressions;
using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class DocumentationExampleTests
{
    [Fact]
    public void EveryReadmeInputConvertsOrProvidesTheDocumentedIncompleteInputGuidance()
    {
        var readme = ReadDocumentation("readme.md");
        var queries = Regex.Matches(readme, @"`(?<query>[^`\r\n]+)`|^\s*- ""(?<query>[^""]+)""\s*$", RegexOptions.Multiline)
            .Select(match => match.Groups["query"].Value)
            .Where(query => Regex.IsMatch(query, @"^[-(]?\d") && Regex.IsMatch(query, "[\\p{L}°'\"]"))
            // Number-format samples have no unit, even if they contain an exponent or apostrophe.
            .Where(query => !Regex.IsMatch(query, @"^[+-]?\d+(?:[.,]\d+|['’]\d{3})*(?:[eE][+-]?\d+)?$"))
            .Distinct().ToArray();
        Assert.NotEmpty(queries);

        var service = new ConversionService();
        foreach (var query in queries)
        {
            var culture = CultureInfo.GetCultureInfo(query switch
            {
                "1 pied 2 pouces en centimètres" or "1,5 mètres en pieds" => "fr-FR",
                "1,5 metry na stopy" => "cs-CZ",
                "1メートルをフィートに変換" or "1mをftに" => "ja-JP",
                "1미터를 피트로 변환" or "1m를ft로" => "ko-KR",
                "1米转换为英尺" or "1m转ft" => "zh-CN",
                "1公尺換算成英尺" or "1m轉ft" => "zh-TW",
                _ => "en-US"
            });
            var result = service.ConvertQuery(query, culture);
            if (query == "1 m to")
            {
                Assert.False(result.Success);
                Assert.True(result.IsIncomplete);
                Assert.NotEmpty(result.Completions);
                Assert.All(result.Completions, completion => Assert.True(service.ConvertQuery(completion.Query, culture).Success, completion.Query));
            }
            else
            {
                Assert.True(result.Success, $"{query}: {result.ErrorMessage}");
                Assert.True(double.IsFinite(result.Output.Value), query);
                Assert.NotEqual(UnitType.Unknown, result.Output.Unit.Type);
            }
        }
    }

    [Fact]
    public void EveryCatalogExampleProducesItsDocumentedResult()
    {
        var examples = Regex.Matches(ReadDocumentation("units.md"), @"^\| `(?<query>[^`]+)` \| `(?<result>[^`]+)` \|\r?$", RegexOptions.Multiline);
        Assert.NotEmpty(examples);
        var service = new ConversionService();
        foreach (Match example in examples)
        {
            var query = example.Groups["query"].Value;
            var result = service.ConvertQuery(query, CultureInfo.GetCultureInfo("en-US"));
            Assert.True(result.Success, $"{query}: {result.ErrorMessage}");
            Assert.Equal(example.Groups["result"].Value, result.FormattedResult);
        }
    }

    [Fact]
    public void EveryDocumentedUnitSymbolAndAliasConvertsInBothDirections()
    {
        var service = new ConversionService();
        var culture = CultureInfo.GetCultureInfo("en-US");
        var modules = service.Modules.ToDictionary(module => ConverterText.Get(module.Category, culture));
        var failures = new List<string>();
        var category = string.Empty;
        var unitCount = 0;

        foreach (var line in ReadDocumentation("units.md").Split('\n'))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                category = line[3..].Trim();
            }
            if (!line.StartsWith('`') || !line.Contains('|'))
            {
                continue;
            }

            var cells = line.Split('|').Select(cell => cell.Trim().Trim('`')).ToArray();
            Assert.Equal(7, cells.Length);
            Assert.True(modules.TryGetValue(category, out var module), category);
            var id = cells[0];
            Assert.Contains(id, module.GetSupportedUnits().Keys);
            unitCount++;
            var reference = module.GetSupportedUnits().Keys.First(unit => unit != id);
            var tokens = new[] { id, cells[1] }.Concat(cells[5].Split(", "))
                .Where(token => !string.IsNullOrWhiteSpace(token)).Distinct(StringComparer.Ordinal);

            foreach (var token in tokens)
            {
                var implicitResult = service.ConvertQuery($"1 {token}", culture);
                var interpretations = new[] { implicitResult }.Concat(implicitResult.OtherInterpretations.Select(
                    interpretation => service.ConvertQuery(interpretation.Query, culture)));
                if (!interpretations.Any(result => result.Success && result.Output.Value == 1 &&
                    result.Output.Unit.Type == Enum.Parse<UnitType>(module.Category) &&
                    module.TryNormalizeUnit(result.Output.Unit.Unit +
                        (result.Output.Unit.Dimension == UnitDimension.Squared ? "2" : string.Empty), culture, out var normalized) && normalized == id))
                {
                    failures.Add($"[{category}] 1 {token}: expected {id}, got " +
                        (implicitResult.Success ? $"{implicitResult.Output.Value:R} {implicitResult.Output.Unit.Unit}" : implicitResult.ErrorMessage));
                }
                Check($"1 {token} to {reference}", id, reference);
                Check($"1 {reference} to {token}", reference, id);
            }

            void Check(string query, string from, string to)
            {
                Assert.True(module.TryConvert(1, from, to, out var expected), $"{category}: {from} to {to}");
                var result = service.ConvertQuery(query, culture);
                if (!result.Success || !double.IsFinite(result.Output.Value) ||
                    Math.Abs(result.Output.Value - expected) > Math.Abs(expected) * 1e-10)
                {
                    failures.Add($"[{category}] {query}: expected {expected:R}, got " +
                        (result.Success ? $"{result.Output.Value:R} {result.Output.Unit.Unit}" : result.ErrorMessage));
                }
            }
        }

        Assert.Equal(service.GetAllSupportedUnits().Values.Sum(units => units.Count), unitCount);
        Assert.True(failures.Count == 0, string.Join(Environment.NewLine, failures));
    }

    private static string ReadDocumentation(string name)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "JPSoftworks.UnitConverterExtension.slnx")))
        {
            root = root.Parent;
        }

        Assert.NotNull(root);
        return File.ReadAllText(Path.Combine(root.FullName, name));
    }
}