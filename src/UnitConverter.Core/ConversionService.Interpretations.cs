using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core;

public partial class ConversionService
{
    private readonly ConcurrentDictionary<string, Dictionary<string, List<UnitSpelling>>> _unitSpellings = new();

    private ConversionResult EvaluateQuery(string query, CultureInfo culture, ConversionWordLanguage wordLanguage)
    {
        var uiCulture = this._uiCulture ?? culture;
        var original = EvaluateExactQuery(query, culture, wordLanguage);
        var parsed = ConversionQueryParser.TryParseQuery(query, culture, this._modules, wordLanguage);
        if (parsed == null)
            return original;

        query = parsed.OriginalQuery;
        var inputs = GetUnitInputs(query, parsed, culture, wordLanguage);
        if (inputs == null || inputs.Count == 0)
            return original;

        var preferredMatches = inputs.Select(input => input.Matches[0]).ToArray();
        var hadPreferred = inputs.All(input => input.Matches.Length == 1 || input.Matches[0].Rank < 2);
        var types = inputs[0].Matches.Select(match => match.Type).ToHashSet();
        foreach (var input in inputs.Skip(1))
        {
            types.IntersectWith(input.Matches.Select(match => match.Type));
        }

        if (types.Count == 0)
            return original;

        for (var i = 0; i < inputs.Count; i++)
        {
            var matches = inputs[i].Matches.Where(match => types.Contains(match.Type)).ToArray();
            // Resolve uncertain spellings before offering optional casing variants of exact inputs.
            inputs[i] = inputs[i] with { Matches = !hadPreferred && matches[0].Rank < 2 ? [matches[0]] : matches };
        }

        if (original.Success && inputs.All(input => input.Matches.Length == 1))
            return original;

        var hasPreferred = hadPreferred && inputs
            .Select((input, index) => input.Matches.Contains(preferredMatches[index])).All(matches => matches);
        var selected = inputs.Select(input => input.Matches[0]).ToArray();
        var preferredQuery = RewriteUnits();
        var attempts = 0;
        var truncated = false;
        var variants = Enumerate(0).Take(4).ToArray();
        if (variants.Length == 0)
            return original.Success && !hadPreferred ? new ConversionResult
            {
                IsIncomplete = true,
                ErrorMessage = ConverterText.Get("Choose a unit interpretation.", uiCulture)
            } : original;

        var preferred = variants.FirstOrDefault(variant => variant.Query == preferredQuery);
        var main = hadPreferred && original.Success ? original
            : hasPreferred && preferred.Result != null ? preferred.Result
            : !hadPreferred && variants.Length == 1 && !truncated ? variants[0].Result
            : null;
        var mainQuery = main == null ? null : hasPreferred ? preferredQuery : variants[0].Query;
        var alternatives = variants.Where(variant => variant.Query != mainQuery).Take(3).Select(variant =>
        {
            var suggestion = variant.Result.Suggestions.FirstOrDefault();
            return new QueryCompletion(variant.Query, suggestion?.Output.Unit ?? variant.Result.Output.Unit)
            {
                FormattedResult = suggestion?.FormattedResult ?? variant.Result.FormattedResult
            };
        }).ToArray();

        return main != null ? main with { OtherInterpretations = alternatives }
            : hadPreferred ? original with { OtherInterpretations = alternatives } : new ConversionResult
        {
            IsIncomplete = true,
            ErrorMessage = ConverterText.Get("Choose a unit interpretation.", uiCulture),
            OtherInterpretations = alternatives
        };

        string RewriteUnits()
        {
            var rewritten = new StringBuilder(query);
            for (var i = inputs.Count - 1; i >= 0; i--)
            {
                var input = inputs[i];
                var match = selected[i];
                if (match == input.Matches[0] && match.AcceptsInput && (match.Rank < 2 || input.Matches.Length == 1))
                {
                    continue;
                }

                rewritten.Remove(input.Start, input.Length).Insert(input.Start, match.Unit);
            }
            return rewritten.ToString();
        }

        IEnumerable<(string Query, ConversionResult Result)> Enumerate(int index)
        {
            if (index == inputs.Count)
            {
                // shortcut: bound casing searches to 64 combinations; use staged choices if larger mixed queries need them.
                if (attempts++ >= 64)
                {
                    truncated = true;
                    yield break;
                }
                var rewritten = RewriteUnits();
                var conversion = EvaluateExactQuery(rewritten, culture, wordLanguage);
                if (conversion.Success)
                {
                    yield return (rewritten, conversion);
                }

                yield break;
            }

            foreach (var match in inputs[index].Matches)
            {
                if (truncated)
                {
                    yield break;
                }

                if (index > 0 && match.Type != selected[0].Type)
                {
                    continue;
                }

                selected[index] = match;
                foreach (var variant in Enumerate(index + 1))
                {
                    yield return variant;
                }
            }
        }
    }

    private List<UnitInput>? GetUnitInputs(string query, ParsedConversionQuery parsed, CultureInfo culture, ConversionWordLanguage wordLanguage)
    {
        var inputs = new List<UnitInput>();
        var (connector, targetEnd) = ConversionQueryParser.FindConversionBoundary(query, culture, this._modules, wordLanguage);
        var sourceEnd = parsed.IsImplicitConversion ? query.Length : connector.Index;
        var sourceText = query[..sourceEnd];
        var sourceUnits = parsed.Components.Length > 0 ? parsed.Components.Select(component => component.Unit).ToArray()
            : [new UnitToken { Unit = parsed.FromUnit, Dimension = parsed.FromDimension }];
        foreach (var unit in sourceUnits.Reverse())
        {
            var start = sourceEnd == 0 ? -1 : query.LastIndexOf(unit.Unit, sourceEnd - 1, sourceEnd, StringComparison.Ordinal);
            if (start < 0)
            {
                if (!EvaluateExactQuery(sourceText, culture, wordLanguage).Success)
                    return null;
                inputs.Clear();
                break;
            }
            if (!AddInput(start, unit.Unit, unit.Dimension))
                return null;
            sourceEnd = start;
        }

        if (!parsed.IsImplicitConversion)
        {
            var start = targetEnd == 0 ? -1 : query.LastIndexOf(parsed.ToUnit, targetEnd - 1,
                targetEnd - connector.Index - connector.Length, StringComparison.Ordinal);
            if (start < connector.Index + connector.Length)
                return null;
            if (!AddInput(start, parsed.ToUnit, parsed.ToDimension))
            {
                var parts = Regex.Matches(parsed.ToUnit, @"\S+");
                if (parts.Count is < 2 or > 4 || parts.Any(part => !AddInput(start + part.Index, part.Value, parsed.ToDimension)))
                    return null;
            }
        }

        return inputs.OrderBy(input => input.Start).ToList();

        bool AddInput(int start, string unit, UnitDimension dimension)
        {
            var matches = GetUnitMatches(unit, dimension, parsed.Dimension, culture);
            if (matches.Length == 0)
                return false;
            inputs.Add(new UnitInput(start, unit.Length, matches));
            return true;
        }
    }

    private UnitMatch[] GetUnitMatches(string input, UnitDimension dimension, UnitDimension queryDimension, CultureInfo culture)
    {
        var spellings = _unitSpellings.GetOrAdd(ConverterText.GetCultureKey(culture), _ => CreateUnitSpellings(culture));
        var candidates = spellings.GetValueOrDefault(input)?.ToList() ?? [];
        var ambiguousName = ConverterText.HasAmbiguousUnitName(input, culture);
        foreach (var module in this._modules)
        {
            if (module.TryNormalizeUnit(input, culture, out var unit) &&
                !candidates.Any(candidate => ReferenceEquals(candidate.Module, module) && candidate.Unit == unit))
            {
                candidates.Add(new UnitSpelling(module, unit, string.Empty, [input]));
            }
        }

        return candidates.Where(candidate => SupportsDimension(candidate.Module, dimension) &&
                (candidate.Module is not DescriptorBasedConversionModule descriptor || !descriptor.TryGetAmbiguity(input, out _, out _)))
            .Select(candidate => new UnitMatch(candidate.Unit, ResolveUnitType(candidate.Module, queryDimension),
                candidate.Unit == input || candidate.Symbol == input ? 0 :
                    !ambiguousName && candidate.Names.Contains(input, StringComparer.Ordinal) ? 1 : 2,
                candidate.Module.TryNormalizeUnit(input, culture, out var normalized) && normalized == candidate.Unit))
            .DistinctBy(match => (match.Type, match.Unit))
            .OrderBy(match => match.Rank)
            .ToArray();
    }

    private Dictionary<string, List<UnitSpelling>> CreateUnitSpellings(CultureInfo culture)
    {
        var spellings = new Dictionary<string, List<UnitSpelling>>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in this._modules)
        {
            var metadata = module.GetAllUnitMetadata(culture);
            foreach (var unit in module.GetSupportedUnits())
            {
                var symbol = metadata.GetValueOrDefault(unit.Key)?.Symbol ?? unit.Key;
                string[] names = [unit.Key, symbol, .. unit.Value, .. ConverterText.GetUnitNames(module.Category, unit.Key, culture)];
                var spelling = new UnitSpelling(module, unit.Key, symbol, names);
                foreach (var name in names.Where(name => !string.IsNullOrWhiteSpace(name)).Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!spellings.TryGetValue(name, out var matches))
                    {
                        spellings[name] = matches = [];
                    }

                    matches.Add(spelling);
                }

                // Preserve pint and parsec defaults while offering their CSS meanings.
                if (module is UnitsNetLengthModule && unit.Key is "dtp pt" or "dtp pica")
                {
                    var abbreviation = unit.Key == "dtp pt" ? "pt" : "pc";
                    if (!spellings.TryGetValue(abbreviation, out var matches))
                    {
                        spellings[abbreviation] = matches = [];
                    }
                    matches.Add(spelling with { Names = [] });
                }
            }
        }
        return spellings;
    }

    private sealed record UnitSpelling(IConversionModule Module, string Unit, string Symbol, string[] Names);
    private sealed record UnitMatch(string Unit, UnitType Type, int Rank, bool AcceptsInput);
    private sealed record UnitInput(int Start, int Length, UnitMatch[] Matches);
}
