// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core;

/// <summary>
/// Provides ambiguity information for human-friendly error messages when a token
/// can refer to multiple units within a module/category.
/// </summary>
public interface IAmbiguityProvider
{
    /// <summary>
    /// Returns true if the provided token is ambiguous in this module and provides
    /// the list of candidate unit ids and a human-readable reason message.
    /// </summary>
    bool TryGetAmbiguity(string token, out string[] candidates, out string reasonMessage);

    /// <summary>
    /// Returns all ambiguous tokens known to this provider.
    /// </summary>
    IEnumerable<string> GetAmbiguousTokens();
}
