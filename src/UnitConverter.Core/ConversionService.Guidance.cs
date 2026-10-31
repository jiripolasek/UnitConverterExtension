using System.Text.RegularExpressions;

namespace JPSoftworks.UnitConverter.Core;

public partial class ConversionService
{
    private ConversionResult AddGuidance(ConversionResult result, string query, CultureInfo culture, ConversionWordLanguage wordLanguage)
    {
        var uiCulture = this._uiCulture ?? culture;
        if (string.IsNullOrWhiteSpace(query))
            return result with { IsIncomplete = true, ErrorMessage = ConverterText.Get("Enter a value and a unit.", uiCulture) };
        if (query.Length > 4096)
            return result;

        query = query.Trim();
        var (connector, targetEnd) = ConversionQueryParser.FindConversionBoundary(query, culture, this._modules, wordLanguage);

        ConversionResult? source = null;
        string prefix;
        string queryStart;
        string? nativeSource = null;
        CultureInfo? nativeCulture = null;
        var dimension = UnitDimension.Linear;
        if (connector.Success)
        {
            var input = query[..connector.Index].Trim();
            source = EvaluateQuery(input, culture, wordLanguage);
            prefix = query[(connector.Index + connector.Length)..targetEnd].Trim();
            if (connector.Groups["target"].Success)
            {
                nativeSource = input;
                nativeCulture = ConverterText.GetQueryLanguages(culture).FirstOrDefault(language =>
                    ConverterText.GetConversionWords(language, wordLanguage).Contains(connector.Value, StringComparer.OrdinalIgnoreCase)) ?? culture;
            }

            if (!source.Success)
            {
                if (prefix.Length == 0 && source.OtherInterpretations.Length > 0)
                    return result with
                    {
                        IsIncomplete = true,
                        ErrorMessage = source.ErrorMessage,
                        OtherInterpretations = source.OtherInterpretations.Select(interpretation => interpretation with
                        {
                            Query = nativeSource != null ? FormatConversionQuery(interpretation.Query, "", culture, wordLanguage, nativeCulture!)
                                : $"{interpretation.Query} {connector.Value} "
                        }).ToArray()
                    };
                return result;
            }
            queryStart = $"{input} {connector.Value} ";
        }
        else
        {
            if (!NumericExpression.TryRead(query, culture, out _, out var consumed))
            {
                var unfinished = Regex.IsMatch(query, @"^[\d(.,+\-].*[+\-*/×÷(]$");
                return unfinished ? result with
                {
                    IsIncomplete = true,
                    ErrorMessage = ConverterText.Get("Complete the calculation, then enter a unit.", uiCulture)
                } : result;
            }
            prefix = query[consumed..].Trim();
            queryStart = query[..consumed].TrimEnd() + " ";
            var parsed = ConversionQueryParser.TryParseQuery(query, culture, this._modules, wordLanguage);
            if (parsed != null)
            {
                prefix = parsed.FromUnit;
                dimension = parsed.FromDimension;
                if (parsed.Components.Length > 1)
                    return result;
            }
        }

        var candidates = new List<(string Id, UnitToken Token, string[] Names)>();
        foreach (var module in this._modules)
        {
            var targetDimension = source?.Output.Unit.Type switch
            {
                UnitType.Area when module.SupportsSquaredUnits => UnitDimension.Squared,
                UnitType.Volume when module.SupportsCubicUnits => UnitDimension.Cubic,
                _ => dimension
            };
            if (!SupportsDimension(module, targetDimension) ||
                (source != null && ResolveUnitType(module, targetDimension) != source.Output.Unit.Type))
            {
                continue;
            }

            foreach (var unit in module.GetSupportedUnits())
            {
                var metadata = module.GetUnitMetadata(unit.Key, culture);
                var suffix = targetDimension switch { UnitDimension.Squared => "²", UnitDimension.Cubic => "³", _ => "" };
                var token = new UnitToken { Unit = unit.Key, Type = ResolveUnitType(module, targetDimension), Dimension = targetDimension,
                    Metadata = uiCulture.Equals(culture) ? metadata : module.GetUnitMetadata(unit.Key, uiCulture) };
                candidates.Add((unit.Key + suffix, token,
                    [unit.Key + suffix, metadata?.DisplayName ?? unit.Key, .. unit.Value, .. ConverterText.GetUnitNames(module.Category, unit.Key, culture)]));
            }
        }

        var matches = candidates.Where(candidate => candidate.Names.Any(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))).ToArray();
        var incomplete = prefix.Length == 0 || matches.Length > 0;
        var completions = (matches.Length > 0 ? matches : candidates.ToArray())
            .OrderBy(candidate => source == null || !source.Suggestions.Any(suggestion => suggestion.Output.Unit.Unit == candidate.Token.Unit))
            .ThenBy(candidate => candidate.Token.Metadata?.IsCommon != true)
            .ThenBy(candidate => candidate.Id.Length)
            .Select(candidate =>
            {
                var completedQuery = nativeSource != null ? FormatConversionQuery(nativeSource, candidate.Id, culture, wordLanguage, nativeCulture!)
                    : queryStart + candidate.Id;
                var conversion = EvaluateQuery(completedQuery, culture, wordLanguage);
                if (!conversion.Success || (source != null && prefix.Length == 0 &&
                    conversion.Output.Unit.Unit == source.Output.Unit.Unit &&
                    conversion.Output.Unit.Dimension == source.Output.Unit.Dimension))
                    return null;

                return new QueryCompletion(completedQuery, source == null ? candidate.Token : conversion.Output.Unit)
                {
                    FormattedResult = source == null ? string.Empty : conversion.FormattedResult
                };
            })
            .OfType<QueryCompletion>()
            .Take(8)
            .ToArray();

        return result with
        {
            IsIncomplete = incomplete && completions.Length > 0,
            ErrorMessage = incomplete && completions.Length > 0
                ? ConverterText.Get(source == null ? "Choose a source unit." : "Choose a compatible target unit.", uiCulture)
                : result.ErrorMessage,
            Completions = completions
        };
    }
}
