// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Globalization;
using System.Text;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Pages;

internal sealed partial class HelpPage : ContentPage
{
    private readonly ISettingsManager? _settings;

    public HelpPage(ISettingsManager? settings = null)
    {
        this.Id = "com.jpsoftworks.cmdpal.unitconverter.help";
        this.Title = ConverterText.Get("Help");
        this.Name = this.Title;
        this.Icon = Icons.Help;
        this._settings = settings;
    }

    public override IContent[] GetContent() => [new MarkdownContent(BuildSupportedUnitsMarkdown(this._settings))];

    private static string BuildSupportedUnitsMarkdown(ISettingsManager? settings)
    {
        var culture = CultureInfo.CurrentUICulture;
        var input = UnitConverterExtensionPage.ResolveCulture(settings?.ParsingCulture ?? PreferredCulture.Default);
        var includeUiLanguage = settings?.ParsingCulture != PreferredCulture.ForceEnglishCulture &&
            (settings?.ConversionWordLanguage ?? ConversionWordLanguage.Default) is
                ConversionWordLanguage.Default or ConversionWordLanguage.InterfaceInputAndEnglish;
        CultureInfo[] aliasCultures = includeUiLanguage ? [input, culture] : [input];
        var sb = new StringBuilder($"# {ConverterText.Get("Help")}\n\n## {ConverterText.Get("Overview of Supported Units")}\n\n");
        foreach (var module in new ConversionService().Modules.OrderBy(module => ConverterText.Get(module.Category)))
        {
            sb.AppendLine(culture, $"### {ConverterText.Get(module.Category)}");
            sb.AppendLine();
            foreach (var unit in module.GetSupportedUnits().OrderBy(unit => unit.Key, StringComparer.Ordinal))
            {
                var metadata = module.GetUnitMetadata(unit.Key, culture);
                var aliases = unit.Value.Concat(aliasCultures.SelectMany(language =>
                    ConverterText.GetUnitNames(module.Category, unit.Key, language))).Distinct();
                sb.AppendLine(culture, $"- **{unit.Key}** - {metadata?.DisplayName}");
                sb.AppendLine(culture, $"    - {string.Join(", ", aliases.Select(alias => $"`{alias}`"))}");
                if (!string.IsNullOrEmpty(metadata?.Description))
                {
                    sb.AppendLine(culture, $"    - {metadata.Description}");
                }

                if (!string.IsNullOrEmpty(metadata?.System))
                {
                    sb.AppendLine(culture, $"    - {metadata.System}");
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }
}