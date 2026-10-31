// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Globalization;
using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core;

public enum UnitsNetIdStrategy
{
    Abbreviation,
    EnumName
}

public sealed class UnitsNetDescriptorOptions
{
    public UnitsNetIdStrategy IdStrategy { get; init; } = UnitsNetIdStrategy.Abbreviation;
    public IReadOnlyCollection<string> CommonUnitIds { get; init; } = [];
    public IReadOnlyCollection<UnitDescriptor> GlobalOverrides { get; init; } = [];
    public IReadOnlyDictionary<string, IReadOnlyCollection<UnitDescriptor>> CultureOverrides { get; init; } =
        new Dictionary<string, IReadOnlyCollection<UnitDescriptor>>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, string> AbbreviationOverrides { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public static class UnitsNetDescriptorProvider
{
    // Use an independent cache so descriptor lookup does not initialize the unused global reflection converter.
    private static readonly UnitAbbreviationsCache _abbreviationsCache = UnitAbbreviationsCache.CreateDefault();

    public static IReadOnlyList<UnitDescriptor> CreateDescriptors(
        UnitType unitType,
        CultureInfo culture,
        UnitsNetDescriptorOptions? options = null,
        IEnumerable<UnitDescriptor>? customDescriptors = null)
    {
        var effectiveOptions = options ?? new UnitsNetDescriptorOptions();
        var descriptors = new Dictionary<string, UnitDescriptor>(StringComparer.Ordinal);

        foreach (var descriptor in BuildUnitsNetDescriptors(unitType, culture, effectiveOptions))
        {
            descriptors[descriptor.Id] = descriptor;
        }

        if (customDescriptors != null)
        {
            foreach (var custom in customDescriptors)
            {
                descriptors[custom.Id] = custom;
            }
        }

        ApplyOverrides(descriptors, effectiveOptions.GlobalOverrides);
        ApplyOverrides(descriptors, GetCultureOverrides(culture, effectiveOptions));

        return descriptors.Values.ToArray();
    }

    private static IEnumerable<UnitDescriptor> BuildUnitsNetDescriptors(
        UnitType unitType,
        CultureInfo culture,
        UnitsNetDescriptorOptions options)
    {
        return unitType switch
        {
            UnitType.Length => BuildDescriptors<LengthUnit>(culture, options),
            UnitType.Area => BuildDescriptors<AreaUnit>(culture, options),
            UnitType.Volume => BuildDescriptors<VolumeUnit>(culture, options),
            UnitType.Speed => BuildDescriptors<SpeedUnit>(culture, options),
            UnitType.Time => BuildDescriptors<DurationUnit>(culture, options),
            UnitType.Power => BuildDescriptors<PowerUnit>(culture, options),
            UnitType.Temperature => BuildDescriptors<TemperatureUnit>(culture, options),
            UnitType.Mass => BuildDescriptors<MassUnit>(culture, options),
            UnitType.Energy => BuildDescriptors<EnergyUnit>(culture, options),
            UnitType.Pressure => BuildDescriptors<PressureUnit>(culture, options),
            UnitType.Angle => BuildDescriptors<AngleUnit>(culture, options),
            UnitType.Data => BuildDescriptors<InformationUnit>(culture, options),
            _ => []
        };
    }

    private static IEnumerable<UnitDescriptor> BuildDescriptors<TUnit>(CultureInfo culture, UnitsNetDescriptorOptions options)
        where TUnit : struct, Enum
    {
        foreach (var unit in Enum.GetValues<TUnit>())
        {
            var enumName = unit.ToString();
            var hasAbbreviationOverride = options.AbbreviationOverrides.TryGetValue(enumName, out var overrideSymbol);
            var symbol = overrideSymbol ?? _abbreviationsCache.GetDefaultAbbreviation(unit, culture) ?? enumName;
            var displayName = SplitWords(enumName);
            var aliasList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                aliasList.Add(displayName);
            }

            // Replaced abbreviations may be reserved by another unit or category.
            if (!hasAbbreviationOverride)
            {
                foreach (var alias in _abbreviationsCache.GetUnitAbbreviations(unit, culture))
                {
                    aliasList.Add(alias);
                }
            }

            var unitId = options.IdStrategy == UnitsNetIdStrategy.EnumName
                ? enumName
                : overrideSymbol ?? _abbreviationsCache.GetDefaultAbbreviation(unit, CultureInfo.InvariantCulture) ?? enumName;

            yield return new UnitDescriptor
            {
                Id = unitId,
                DisplayName = displayName,
                Symbol = symbol,
                System = string.Empty,
                IsCommon = options.CommonUnitIds.Contains(unitId),
                Aliases = aliasList.Where(a => !string.IsNullOrWhiteSpace(a) && !string.Equals(a, unitId, StringComparison.Ordinal))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            };
        }
    }

    private static void ApplyOverrides(Dictionary<string, UnitDescriptor> descriptors, IEnumerable<UnitDescriptor> overrides)
    {
        foreach (var descriptor in overrides)
        {
            if (descriptors.TryGetValue(descriptor.Id, out var existing))
            {
                descriptors[descriptor.Id] = MergeDescriptors(existing, descriptor);
            }
            else
            {
                descriptors[descriptor.Id] = descriptor;
            }
        }
    }

    private static UnitDescriptor MergeDescriptors(UnitDescriptor baseDescriptor, UnitDescriptor overrideDescriptor)
    {
        var aliases = baseDescriptor.Aliases
            .Concat(overrideDescriptor.Aliases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var tags = baseDescriptor.Tags
            .Concat(overrideDescriptor.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var commonTargets = baseDescriptor.CommonTargets
            .Concat(overrideDescriptor.CommonTargets)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return baseDescriptor with
        {
            DisplayName = string.IsNullOrWhiteSpace(overrideDescriptor.DisplayName)
                ? baseDescriptor.DisplayName
                : overrideDescriptor.DisplayName,
            Symbol = string.IsNullOrWhiteSpace(overrideDescriptor.Symbol)
                ? baseDescriptor.Symbol
                : overrideDescriptor.Symbol,
            System = string.IsNullOrWhiteSpace(overrideDescriptor.System)
                ? baseDescriptor.System
                : overrideDescriptor.System,
            Description = string.IsNullOrWhiteSpace(overrideDescriptor.Description)
                ? baseDescriptor.Description
                : overrideDescriptor.Description,
            IsCommon = overrideDescriptor.IsCommon,
            Tags = tags,
            Aliases = aliases,
            CommonTargets = commonTargets,
            SuggestionPriority = overrideDescriptor.SuggestionPriority == baseDescriptor.SuggestionPriority
                ? baseDescriptor.SuggestionPriority
                : overrideDescriptor.SuggestionPriority,
            Properties = baseDescriptor.Properties.Count == 0 && overrideDescriptor.Properties.Count > 0
                ? overrideDescriptor.Properties
                : baseDescriptor.Properties
        };
    }

    private static IEnumerable<UnitDescriptor> GetCultureOverrides(CultureInfo culture, UnitsNetDescriptorOptions options)
    {
        if (options.CultureOverrides.Count == 0)
            return [];

        if (options.CultureOverrides.TryGetValue(culture.Name, out var overrides))
            return overrides;

        if (!string.IsNullOrWhiteSpace(culture.TwoLetterISOLanguageName) &&
            options.CultureOverrides.TryGetValue(culture.TwoLetterISOLanguageName, out overrides))
        {
            return overrides;
        }

        return [];
    }

    private static string SplitWords(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var chars = new List<char>(value.Length * 2) { value[0] };
        for (var i = 1; i < value.Length; i++)
        {
            var current = value[i];
            if (char.IsUpper(current) && !char.IsUpper(value[i - 1]))
            {
                chars.Add(' ');
            }
            chars.Add(current);
        }

        return new string(chars.ToArray());
    }
}