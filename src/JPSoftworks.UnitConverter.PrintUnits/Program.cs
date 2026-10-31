// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Globalization;
using System.Text;
using JPSoftworks.UnitConverter.Core;

internal sealed class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var markdown = BuildSupportedUnitsMarkdown();
        Console.Write(markdown);
    }

    private static string BuildSupportedUnitsMarkdown()
    {
        var service = new ConversionService();
        var culture = CultureInfo.GetCultureInfo("en-US");
        var allUnits = service.GetAllSupportedUnits();
        var sb = new StringBuilder();

        sb.AppendLine("# Unit Converter - Supported Units");
        sb.AppendLine();

        // Build table of contents
        var orderedCategories = allUnits.OrderBy(kvp => kvp.Key).ToList();
        sb.AppendLine("## Table of Contents");
        sb.AppendLine("- [CSS and typography](#css-and-typography)");
        foreach (var cat in orderedCategories)
        {
            var categoryName = ConverterText.Get(cat.Key, culture);
            var anchor = ToAnchor(categoryName);
            sb.AppendLine(culture, $"- [{categoryName}](#{anchor})");
        }
        sb.AppendLine();

        sb.AppendLine("""
            ## CSS and typography

            CSS absolute lengths use `px`, `pt`, `pc`, `Q`, `in`, `cm`, and `mm`.
            Use `dtp pt` or `css pt` for CSS points and `dtp pica` or `css pc` for CSS picas.
            `pt (DTP)`, `pt DTP`, `pt (CSS)`, and `pc (CSS)` are also accepted.
            DTP means desktop publishing.

            Bare `1 pt` keeps the preferred pint and offers `1 dtp pt` under Other interpretations.
            Bare `1 pc` keeps parsec and offers `1 dtp pica` under Other interpretations.
            For explicit CSS conversions, use the qualified spellings below.

            | Input | Result |
            |:--|:--|
            | `16 px to dtp pt` | `12 pt (DTP)` |
            | `12 css pt to px` | `16 px` |
            | `1 css pc to px` | `16 px` |
            | `4 Q to mm` | `1 mm` |
            | `1 in to px` | `96 px` |
            | `2.54 cm to px` | `96 px` |
            | `25.4 mm to px` | `96 px` |

            A CSS pixel is 1/96 inch; these conversions follow the [CSS absolute length definitions](https://www.w3.org/TR/css-values-4/#absolute-lengths) and do not depend on monitor DPI.
            Printer points and picas use the traditional TeX sizes, which differ from DTP/CSS sizes.
            Relative units such as `em`, `rem`, `%`, and viewport units require font or viewport context and are not supported.

            """);

        foreach (var category in orderedCategories)
        {
            sb.AppendLine(culture, $"## {ConverterText.Get(category.Key, culture)}");
            sb.AppendLine();

            // Build a table header for this category
            sb.AppendLine("Unit | Symbol | Name | System | Common | Aliases | Description");
            sb.AppendLine(":--|:--|:--|:--|:-:|:--|:--");

            // Find modules for this category to get richer metadata
            var modules = service.Modules.Where(module => module.Category == category.Key);

            var categoryMetadata = new Dictionary<string, UnitMetadata>();
            foreach (var module in modules)
            {
                try
                {
                    var moduleMetadata = module.GetAllUnitMetadata(culture);
                    foreach (var kvp in moduleMetadata)
                    {
                        categoryMetadata[kvp.Key] = kvp.Value;
                    }
                }
                catch
                {
                    foreach (var unitKey in module.GetSupportedUnits().Keys)
                    {
                        var metadata = UnitMetadataProvider.GetMetadata(unitKey);
                        if (metadata != null)
                        {
                            categoryMetadata[unitKey] = metadata;
                        }
                    }
                }
            }

            foreach (var unit in category.Value.OrderBy(kvp => kvp.Key))
            {
                categoryMetadata.TryGetValue(unit.Key, out var metadata);

                var unitId = $"`{Escape(unit.Key)}`";
                var symbol = (metadata != null && !string.IsNullOrWhiteSpace(metadata.Symbol) && metadata.Symbol != unit.Key)
                    ? $"`{Escape(metadata.Symbol)}`"
                    : string.Empty;
                var name = Escape(metadata?.DisplayName ?? unit.Key);
                var system = Escape(metadata?.System ?? string.Empty);
                var common = (metadata?.IsCommon ?? true) ? "⭐" : string.Empty;
                var aliases = unit.Value.Length > 0 ? Escape(string.Join(", ", unit.Value)) : string.Empty;
                var description = Escape(metadata?.Description ?? string.Empty);

                sb.AppendLine($"{unitId} | {symbol} | {name} | {system} | {common} | {aliases} | {description}".TrimEnd());
            }

            sb.AppendLine();
        }

        // Add legend
        sb.AppendLine("---");
        sb.AppendLine("**Legend:**");
        sb.AppendLine("- ⭐ = Commonly used unit");
        sb.AppendLine("- System = Unit system (Metric, Imperial, etc.)");
        sb.AppendLine("- Symbol = Display symbol if different from ID");

        return sb.ToString();

        static string Escape(string? s) => string.IsNullOrEmpty(s) ? string.Empty : s.Replace("|", "\\|");

        static string ToAnchor(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = s.Trim().ToLowerInvariant();
            var anchorBuilder = new StringBuilder();
            var prevHyphen = false;
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    anchorBuilder.Append(ch);
                    prevHyphen = false;
                }
                else if (ch == ' ' || ch == '-' || ch == '_' || ch == '/')
                {
                    if (!prevHyphen)
                    {
                        anchorBuilder.Append('-');
                        prevHyphen = true;
                    }
                }
                // skip other punctuation
            }
            var result = anchorBuilder.ToString().Trim('-');
            return result;
        }
    }
}