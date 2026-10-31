// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension;

internal sealed partial class UnitConverterExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly IFallbackCommandItem[] _fallbackCommands;
    private readonly SettingsManager _settingsManager = new();

    public UnitConverterExtensionCommandsProvider()
    {
        this.DisplayName = ConverterText.Get("Unit Converter for Command Palette");
        this.Icon = Icons.AppLogoVector;
        this.Settings = this._settingsManager.Settings;
        var page = new UnitConverterExtensionPage(this._settingsManager);
        this._fallbackCommands = [new FallbackConversionItem(page)];
        this._commands =
        [
            new CommandItem(page)
            {
                Title = ConverterText.Get("Convert units"),
                Subtitle = ConverterText.Get("Convert common units of length, volume, temperature, weight, time or speed"),
                MoreCommands = [
                    new CommandContextItem(this._settingsManager.Settings.SettingsPage)
                    ]
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return this._commands;
    }

    public override IFallbackCommandItem[] FallbackCommands() => this._fallbackCommands;
}