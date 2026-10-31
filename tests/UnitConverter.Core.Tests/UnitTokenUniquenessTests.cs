// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverter.Core.Modules;

namespace JPSoftworks.UnitConverter.Core.Tests;

public class UnitTokenUniquenessTests
{
    private static IEnumerable<IConversionModule> GetAllModules()
    {
        return new ConversionService().Modules;
    }

    [Fact]
    public void AllModulesTokensAreUniqueAndRoundTripToOwner()
    {
        foreach (var module in GetAllModules())
        {
            var supported = module.GetSupportedUnits();
            var tokenOwners = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var (unitId, aliases) in supported)
            {
                var md = module.GetUnitMetadata(unitId, CultureInfo.InvariantCulture);
                var tokens = new List<string> { unitId };

                if (md != null && !string.IsNullOrWhiteSpace(md.Symbol) && !string.Equals(md.Symbol, unitId, StringComparison.Ordinal))
                {
                    tokens.Add(md.Symbol);
                }

                if (aliases is { Length: > 0 })
                {
                    tokens.AddRange(aliases.Where(a => !string.IsNullOrWhiteSpace(a))!);
                }

                foreach (var token in tokens)
                {
                    // Allow modules to block ambiguous tokens explicitly
                    if (module is DescriptorBasedConversionModule desc && desc.TryGetAmbiguity(token, out var candidates, out var reason))
                    {
                        // Ambiguous tokens must not normalize; they should be rejected with guidance
                        var ambiguousOk = module.TryNormalizeUnit(token, CultureInfo.InvariantCulture, out _);
                        Assert.False(ambiguousOk, $"[{module.Category}] Ambiguous token '{token}' should not normalize. {reason} Candidates: {string.Join(", ", candidates)}");
                        // Skip uniqueness and round-trip checks for ambiguous tokens
                        continue;
                    }

                    // 1) Token must normalize successfully
                    var ok = module.TryNormalizeUnit(token, CultureInfo.InvariantCulture, out var normalized);
                    Assert.True(ok, $"[{module.Category}] Token '{token}' should normalize but did not.");

                    // 2) Token must normalize back to its owning unit id
                    var isCaseSensitive = module switch
                    {
                        ConversionModuleBase baseModule => baseModule.IsCaseSensitive,
                        DescriptorBasedConversionModule descriptorModule => descriptorModule.IsCaseSensitive,
                        _ => true
                    };

                    if (isCaseSensitive)
                    {
                        Assert.Equal(unitId, normalized);
                    }
                    else
                    {
                        Assert.True(string.Equals(unitId, normalized, StringComparison.OrdinalIgnoreCase),
                            $"[{module.Category}] Token '{token}' normalized to '{normalized}', expected '{unitId}'.");
                    }

                    // 3) Token literal must be unique within the module (no duplicate id/symbol/alias strings)
                    if (tokenOwners.TryGetValue(token, out var existingOwner))
                    {
                        Assert.True(string.Equals(existingOwner, unitId, StringComparison.Ordinal),
                            $"[{module.Category}] Token '{token}' is defined for multiple units: '{existingOwner}' and '{unitId}'.");
                    }
                    else
                    {
                        tokenOwners[token] = unitId;
                    }
                }
            }
        }
    }

    [Fact]
    public void AllModulesAmbiguousTokensAreNotUsedAsIdsOrAliases()
    {
        foreach (var module in GetAllModules())
        {
            if (module is not DescriptorBasedConversionModule desc)
            {
                continue;
            }

            var ambiguous = new HashSet<string>(desc.GetAmbiguousTokens(), StringComparer.OrdinalIgnoreCase);
            if (ambiguous.Count == 0)
            {
                continue; // No ambiguity declared for this module
            }

            var supported = module.GetSupportedUnits();

            foreach (var (unitId, aliases) in supported)
            {
                // No ambiguous token may be used as a unit id (case-insensitive compare for safety)
                Assert.False(ambiguous.Contains(unitId),
                    $"[{module.Category}] Ambiguous token '{unitId}' must not be used as a unit id.");

                // No ambiguous token may be used as an alias
                foreach (var alias in aliases)
                {
                    Assert.False(ambiguous.Contains(alias),
                        $"[{module.Category}] Ambiguous token '{alias}' must not be used as an alias for '{unitId}'.");
                }

                // Also verify symbols from metadata are not ambiguous if symbol differs from id
                var md = module.GetUnitMetadata(unitId, CultureInfo.InvariantCulture);
                if (md != null && !string.IsNullOrWhiteSpace(md.Symbol) && !string.Equals(md.Symbol, unitId, StringComparison.Ordinal))
                {
                    Assert.False(ambiguous.Contains(md.Symbol),
                        $"[{module.Category}] Ambiguous token '{md.Symbol}' must not be used as a symbol for '{unitId}'.");
                }
            }
        }
    }

    [Fact]
    public void AllModulesTokensDoNotConflictUsingModuleCaseRules()
    {
        foreach (var module in GetAllModules())
        {
            var supported = module.GetSupportedUnits();
            var comparer = module is DescriptorBasedConversionModule { IsCaseSensitive: true } or ConversionModuleBase { IsCaseSensitive: true }
                ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            var aliasOwners = new Dictionary<string, HashSet<string>>(comparer);
            var idOwners = new Dictionary<string, string>(comparer);

            // Collect tokens using the module's case rules, including Mbps versus MBps.
            foreach (var (unitId, aliases) in supported)
            {
                // Track ids mapped to their owner.
                if (!idOwners.ContainsKey(unitId))
                {
                    idOwners[unitId] = unitId;
                }

                // Track aliases and check conflicts only among aliases and alias vs id
                if (aliases is { Length: > 0 })
                {
                    foreach (var a in aliases)
                    {
                        if (string.IsNullOrWhiteSpace(a))
                        {
                            continue;
                        }

                        // Skip tokens explicitly marked ambiguous by the module
                        if (module is DescriptorBasedConversionModule desc && desc.TryGetAmbiguity(a, out _, out _))
                        {
                            continue;
                        }

                        if (!aliasOwners.TryGetValue(a, out var owners))
                        {
                            owners = new HashSet<string>(StringComparer.Ordinal);
                            aliasOwners[a] = owners;
                        }
                        owners.Add(unitId);
                    }
                }
            }

            // No alias should belong to multiple unit ids.
            foreach (var (alias, owners) in aliasOwners)
            {
                Assert.True(owners.Count <= 1,
                    $"[{module.Category}] Alias '{alias}' is used by multiple units: {string.Join(", ", owners)}");
            }

            // No alias should equal the id of a different unit.
            foreach (var (alias, owners) in aliasOwners)
            {
                if (idOwners.TryGetValue(alias, out var idOwner))
                {
                    // If alias belongs to a unit different from the id owner, that's a conflict (e.g., 'Cal' vs unit id 'cal')
                    var owner = owners.First();
                    Assert.True(string.Equals(owner, idOwner, StringComparison.Ordinal),
                        $"[{module.Category}] Alias '{alias}' conflicts with unit id '{idOwner}'.");
                }
            }
        }
    }
}