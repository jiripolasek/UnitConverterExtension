// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Runtime.InteropServices;
using Microsoft.CommandPalette.Extensions;

namespace JPSoftworks.UnitConverterExtension;

[Guid("a8917dcf-46d9-4398-b9ec-f09249a0abe8")]
public sealed partial class UnitConverterExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;

    private readonly UnitConverterExtensionCommandsProvider _provider = new();

    public UnitConverterExtension(ManualResetEvent extensionDisposedEvent)
    {
        this._extensionDisposedEvent = extensionDisposedEvent;
    }

    public object? GetProvider(ProviderType providerType)
    {
        return providerType switch
        {
            ProviderType.Commands => this._provider,
            _ => null
        };
    }

    public void Dispose() => this._extensionDisposedEvent.Set();
}