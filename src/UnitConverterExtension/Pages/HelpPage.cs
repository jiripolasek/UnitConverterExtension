// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Text;
using JPSoftworks.UnitConverter.Core;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Pages;

internal sealed partial class HelpPage : ContentPage
{
    private readonly MarkdownContent _content;
    private readonly MarkdownContent _head;

    public HelpPage()
    {
        string listOfUnits = BuildSupportedUnitsMarkdown();

        this.Id = "com.jpsoftworks.cmdpal.unitconverter.help";
        this.Title = "Help";
        this.Name = "Help";
        this.Icon = Icons.Help;

        this._head = new MarkdownContent("""
                                         # Help

                                         ## Overview of Supported Units
                                         """);
        this._content = new MarkdownContent(listOfUnits);
    }

    public override IContent[] GetContent()
    {
        return [this._head, this._content];
    }

    private static string BuildSupportedUnitsMarkdown()
    {
        var service = new ConversionService();
        var allUnits = service.GetAllSupportedUnits();
        var sb = new StringBuilder();

        foreach (var category in allUnits.OrderBy(kvp => kvp.Key))
        {
            sb.AppendLine($"### {category.Key}");
            sb.AppendLine();

            var modules = service.Modules;

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
                var metadata = categoryMetadata.GetValueOrDefault(unit.Key);
                var aliases = unit.Value.Length > 0 ? $" ({string.Join(", ", unit.Value.Select(v => "`" + v + "`"))})" : "";

                if (metadata != null)
                {
                    var displayName = !string.IsNullOrEmpty(metadata.DisplayName) ? metadata.DisplayName : unit.Key;

                    sb.AppendLine($"""
                                   - **{unit.Key}** - {displayName}
                                       - `{metadata.Symbol}`, {aliases}
                                   """);

                    if (!string.IsNullOrEmpty(metadata.Description))
                    {
                        sb.AppendLine($"    - {metadata.Description}");
                    }
                    if (!string.IsNullOrEmpty(metadata.System))
                    {
                        sb.AppendLine($"    - {metadata.System} system");
                    }

                }
                else
                {
                    sb.AppendLine($"- **{unit.Key}**{aliases}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
