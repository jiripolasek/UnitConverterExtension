// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Text;
using JPSoftworks.UnitConverter.Core;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        var markdown = BuildSupportedUnitsMarkdown();
        Console.WriteLine(markdown);
    }

    private static string BuildSupportedUnitsMarkdown()
    {
        var service = new ConversionService();
        var allUnits = service.GetAllSupportedUnits();
        var sb = new StringBuilder();

        sb.AppendLine("# Unit Converter - Supported Units");
        sb.AppendLine();

        // Build table of contents
        var orderedCategories = allUnits.OrderBy(kvp => kvp.Key).ToList();
        sb.AppendLine("## Table of Contents");
        foreach (var cat in orderedCategories)
        {
            var anchor = ToAnchor(cat.Key);
            sb.AppendLine($"- [{cat.Key}](#{anchor})");
        }
        sb.AppendLine();

        foreach (var category in orderedCategories)
        {
            sb.AppendLine($"## {category.Key}");
            sb.AppendLine();

            // Build a table header for this category
            sb.AppendLine("Unit | Symbol | Name | System | Common | Aliases | Description");
            sb.AppendLine(":--|:--|:--|:--|:-:|:--|:--");

            // Find modules for this category to get richer metadata
            var modules = typeof(ConversionService).Assembly
                .GetTypes()
                .Where(t => !t.IsAbstract && typeof(IConversionModule).IsAssignableFrom(t))
                .Select(t => Activator.CreateInstance(t) as IConversionModule)
                .Where(m => m != null && m.Category == category.Key)
                .ToList();

            var categoryMetadata = new Dictionary<string, UnitMetadata>();
            foreach (var module in modules)
            {
                try
                {
                    var moduleMetadata = module.GetAllUnitMetadata();
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

                sb.AppendLine($"{unitId} | {symbol} | {name} | {system} | {common} | {aliases} | {description}");
            }

            sb.AppendLine();
        }

        // Add legend
        sb.AppendLine("---");
        sb.AppendLine("**Legend:**");
        sb.AppendLine("- ⭐ = Commonly used unit");
        sb.AppendLine("- System = Unit system (Metric, Imperial, etc.)");
        sb.AppendLine("- Symbol = Display symbol if different from ID");
        sb.AppendLine();

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