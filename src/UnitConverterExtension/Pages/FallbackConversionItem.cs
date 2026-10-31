using JPSoftworks.UnitConverter.Core;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Pages;

// Metadata-based marshaling discovers the fallback identity through this interface chain.
[WinRT.WinRTRuntimeClassName("Microsoft.CommandPalette.Extensions.IFallbackCommandItem2")]
internal sealed partial class FallbackConversionItem : FallbackCommandItem
{
    private readonly UnitConverterExtensionPage _page;
    private const string CommandId = "com.jpsoftworks.cmdpal.unitconverter.fallback";

    public FallbackConversionItem(UnitConverterExtensionPage page)
        : base(new NoOpCommand { Id = CommandId }, ConverterText.Get("Convert units"), CommandId)
    {
        _page = page;
        Title = string.Empty;
        Icon = Icons.AppLogoVector;
    }

    public override void UpdateQuery(string query)
    {
        var result = _page.ConvertQuery(query);
        if (!result.Success && result.OtherInterpretations.Length > 0)
        {
            Title = ConverterText.Get("Choose a unit interpretation.").TrimEnd('.', '。');
            Subtitle = query;
            Command = _page.CreateQueryPage(query, CommandId);
            MoreCommands = [];
            Icon = Icons.Suggestion;
            return;
        }
        if (!result.Success || !double.IsFinite(result.Output.Value))
        {
            Title = string.Empty;
            Subtitle = string.Empty;
            Command = new NoOpCommand { Id = CommandId };
            MoreCommands = [];
            return;
        }

        var suggestion = result.Suggestions.FirstOrDefault();
        var output = suggestion?.Output ?? result.Output;
        var text = suggestion?.FormattedResult ?? result.FormattedResult;
        var item = _page.CreateResultItem(output, text, result.OutputComponents);
        ((CopyTextCommand)item.Command!).Id = CommandId;
        Command = item.Command;
        Title = text;
        Subtitle = query;
        Icon = UnitConverterExtensionPage.GetIcon(output.Unit);
        MoreCommands = [.. item.MoreCommands, new CommandContextItem(_page.CreateQueryPage(query, CommandId + ".open"))];
    }
}