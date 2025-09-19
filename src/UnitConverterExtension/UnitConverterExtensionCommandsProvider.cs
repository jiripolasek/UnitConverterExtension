// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace UnitConverterExtension;

public partial class UnitConverterExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public UnitConverterExtensionCommandsProvider()
    {
        DisplayName = "Unit Converter for Command Palette";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new UnitConverterExtensionPage()) { Title = DisplayName },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
