// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using JPSoftworks.CommandPalette.Extensions.Toolkit;

namespace UnitConverterExtension;

public static class Program
{
    [MTAThread]
    public static async Task Main(string[] args)
    {
        await ExtensionHostRunner.RunAsync(
            args,
            new ExtensionHostRunnerParameters
            {
                PublisherMoniker = "JPSoftworks",
                ProductMoniker = "UnitConverterExtension",
#if DEBUG
                IsDebug = true,
#endif
                EnableEfficiencyMode = true,
                ExtensionFactories = [
                    new DelegateExtensionFactory(manualResetEvent => new UnitConverterExtension(manualResetEvent))
                ]
            });
    }
}
