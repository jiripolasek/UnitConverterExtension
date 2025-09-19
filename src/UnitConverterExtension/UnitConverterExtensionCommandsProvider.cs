// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension;

internal sealed partial class UnitConverterExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly SettingsManager _settingsManager = new();

    public UnitConverterExtensionCommandsProvider()
    {
        this.DisplayName = "Unit Converter for Command Palette";
        this.Icon = Icons.AppLogoVector;
        this.Settings = this._settingsManager.Settings;
        this._commands =
        [
            new CommandItem(new UnitConverterExtensionPage(this._settingsManager))
            {
                Title = "Convert units",
                Subtitle = "Convert common units of length, volume, temperature, weight, time or speed",
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
}