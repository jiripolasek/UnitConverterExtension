// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

using System.Collections.Concurrent;

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Length conversion module powered by Units.NET with support for squared and cubic units using the UnitDescriptor system
/// </summary>
public class UnitsNetLengthModule : DescriptorBasedConversionModule
{
    private readonly UnitsNetDescriptorOptions _descriptorOptions;
    private readonly IReadOnlyCollection<UnitDescriptor> _customDescriptors;
    private readonly ConcurrentDictionary<string, UnitDescriptorCatalog> _localizedCollections =
        new(StringComparer.OrdinalIgnoreCase);

    public UnitsNetLengthModule() : this(null, null)
    {
    }

    public UnitsNetLengthModule(
        UnitsNetDescriptorOptions? descriptorOptions = null,
        IEnumerable<UnitDescriptor>? customDescriptors = null)
        : base(CreateUnitDescriptors(descriptorOptions, customDescriptors))
    {
        _descriptorOptions = CreateDescriptorOptions(descriptorOptions);
        _customDescriptors = (customDescriptors ?? CreateCustomUnitDescriptors()).ToArray();
    }

    public override bool IsCaseSensitive => true;
    public override string Category => "Length";
    public override bool SupportsSquaredUnits => true;
    public override bool SupportsCubicUnits => true;

    protected override UnitType GetUnitType() => UnitType.Length;

    // Physical constants for custom units
    private const double SpeedOfLight = 299_792_458.0; // m/s
    private const double SecondsPerMinute = 60.0;
    private const double SecondsPerHour = 3600.0;
    private const double SecondsPerDay = 86_400.0;
    private const double SecondsPerWeek = 604_800.0;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        try
        {
            if (!TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
                !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            {
                return false;
            }

            var length = CreateLengthDirectly(value, from);
            var converted = ConvertToUnit(length, to);
            if (converted.HasValue)
            {
                result = converted.Value;
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    private static UnitDescriptor[] CreateUnitDescriptors(
        UnitsNetDescriptorOptions? descriptorOptions,
        IEnumerable<UnitDescriptor>? customDescriptors)
    {
        var options = CreateDescriptorOptions(descriptorOptions);
        var custom = customDescriptors ?? CreateCustomUnitDescriptors();
        return UnitsNetDescriptorProvider.CreateDescriptors(UnitType.Length, CultureInfo.InvariantCulture, options, custom).ToArray();
    }

    private static UnitsNetDescriptorOptions CreateDescriptorOptions(UnitsNetDescriptorOptions? options)
    {
        var defaultOverrides = CreateUnitsNetOverrides();
        var defaultAbbreviationOverrides = CreateAbbreviationOverrides();

        return new UnitsNetDescriptorOptions
        {
            IdStrategy = options?.IdStrategy ?? UnitsNetIdStrategy.Abbreviation,
            CommonUnitIds = options?.CommonUnitIds ?? [],
            GlobalOverrides = MergeOverrides(defaultOverrides, options?.GlobalOverrides),
            CultureOverrides = MergeCultureOverrides(options?.CultureOverrides),
            AbbreviationOverrides = MergeAbbreviationOverrides(defaultAbbreviationOverrides, options?.AbbreviationOverrides)
        };
    }

    private static IReadOnlyCollection<UnitDescriptor> MergeOverrides(
        IReadOnlyCollection<UnitDescriptor> defaults,
        IReadOnlyCollection<UnitDescriptor>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return defaults;

        return defaults.Concat(overrides).ToArray();
    }

    private static Dictionary<string, IReadOnlyCollection<UnitDescriptor>> MergeCultureOverrides(
        IReadOnlyDictionary<string, IReadOnlyCollection<UnitDescriptor>>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return new Dictionary<string, IReadOnlyCollection<UnitDescriptor>>(StringComparer.OrdinalIgnoreCase);

        return new Dictionary<string, IReadOnlyCollection<UnitDescriptor>>(overrides, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, string> MergeAbbreviationOverrides(
        IReadOnlyDictionary<string, string> defaults,
        IReadOnlyDictionary<string, string>? overrides)
    {
        if (overrides == null || overrides.Count == 0)
            return defaults;

        var merged = new Dictionary<string, string>(defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in overrides)
        {
            merged[entry.Key] = entry.Value;
        }
        return merged;
    }

    private static Dictionary<string, string> CreateAbbreviationOverrides()
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Angstrom"] = "angstrom",
            ["DataMile"] = "dmi",
            ["DtpPica"] = "dtp pica",
            ["DtpPoint"] = "dtp pt",
            ["Hand"] = "hand",
            ["Microinch"] = "microinch",
            ["Micrometer"] = "um",
            ["NauticalMile"] = "nmi",
            ["PrinterPica"] = "printer pica",
            ["PrinterPoint"] = "printer pt",
            ["UsSurveyFoot"] = "us survey ft"
        };
    }

    /// <summary>
    ///     Preserves existing aliases, suggestion targets, and disambiguated symbols
    ///     while using Units.NET for the base catalog.
    /// </summary>
    private static IReadOnlyCollection<UnitDescriptor> CreateUnitsNetOverrides()
    {
        return
        [
            // --- Metric SI ---
            Override("mm", isCommon: true, system: "Metric", priority: 20,
                aliases: ["millimetre", "millimetres", "millimeters"],
                targets: ["cm", "m", "in"],
                tags: ["SI", "metric", "small", "precision"]),

            Override("cm", isCommon: true, system: "Metric", priority: 15,
                aliases: ["centimetre", "centimetres", "centimeters"],
                targets: ["mm", "m", "in", "ft"],
                tags: ["SI", "metric", "common"]),

            Override("m", isCommon: true, system: "Metric", priority: 5,
                aliases: ["metre", "metres", "meters"],
                targets: ["cm", "ft", "yd", "km"],
                tags: ["SI", "base", "metric", "common"]),

            Override("km", isCommon: true, system: "Metric", priority: 25,
                aliases: ["kilometre", "kilometres", "kilometers", "klick", "klicks", "click", "clicks"],
                targets: ["m", "mi"],
                tags: ["SI", "metric", "distance"]),

            Override("nm", isCommon: true, system: "Metric", priority: 40,
                aliases: ["nanometre", "nanometres", "nanometers"],
                targets: ["um", "m", "angstrom"],
                tags: ["SI", "science"]),

            Override("um", isCommon: true, system: "Metric", priority: 45,
                aliases: ["micrometre", "micrometres", "micrometers", "um"],
                targets: ["mm", "nm", "m"],
                tags: ["SI", "science"], symbol: "µm"),

            // --- Imperial / US ---
            Override("in", isCommon: true, system: "Imperial", priority: 10,
                aliases: ["inch", "inches"],
                targets: ["cm", "mm", "ft"],
                tags: ["imperial", "common", "US"]),

            Override("ft", isCommon: true, system: "Imperial", priority: 8,
                aliases: ["foot", "feet"],
                targets: ["m", "cm", "in", "yd"],
                tags: ["imperial", "common", "US"]),

            Override("yd", isCommon: true, system: "Imperial", priority: 30,
                aliases: ["yard", "yards"],
                targets: ["m", "ft"],
                tags: ["imperial", "US"]),

            Override("mi", isCommon: true, system: "Imperial", priority: 35,
                aliases: ["mile", "miles"],
                targets: ["km", "m"],
                tags: ["imperial", "distance", "US"]),

            // --- Nautical ---
            Override("nmi", isCommon: false, system: "Nautical", priority: 80,
                aliases: ["nautical mile", "nautical miles"],
                targets: ["km", "mi"],
                tags: ["nautical", "maritime", "aviation"]),

            Override("us survey ft", isCommon: false, system: "Imperial", priority: 82,
                aliases: ["us survey foot", "us survey feet", "us ft (survey)", "survey foot", "survey feet"],
                targets: ["ft", "m"],
                tags: ["imperial", "US", "survey"], symbol: "US ft (survey)"),

            Override("ch", isCommon: false, system: "Imperial", priority: 85,
                aliases: ["chain", "chains"],
                targets: ["m", "ft", "yd"],
                tags: ["imperial", "survey"]),

            Override("fathom", isCommon: false, system: "Imperial", priority: 86,
                aliases: ["fathom", "fathoms"],
                targets: ["m", "ft", "yd"],
                tags: ["imperial", "nautical"], symbol: "ftm"),

            Override("hand", isCommon: false, system: "Imperial", priority: 87,
                aliases: ["hand", "hands"],
                targets: ["in", "ft", "cm"],
                tags: ["imperial", "equine"]),

            Override("mil", isCommon: false, system: "Imperial", priority: 88,
                aliases: ["mil", "thou"],
                targets: ["in", "mm"],
                tags: ["imperial", "manufacturing"]),

            Override("microinch", isCommon: false, system: "Imperial", priority: 89,
                aliases: ["microinch", "microinches", "µin", "uin"],
                targets: ["mil", "in", "mm"],
                tags: ["imperial", "precision"], symbol: "µin"),

            Override("kft", isCommon: false, system: "Imperial", priority: 91,
                aliases: ["kilofeet", "kilofoot"],
                targets: ["ft", "mi"],
                tags: ["imperial"]),

            Override("kyd", isCommon: false, system: "Imperial", priority: 91,
                aliases: ["kiloyard", "kiloyards"],
                targets: ["yd", "mi"],
                tags: ["imperial"]),

            Override("dmi", isCommon: false, system: "Customary", priority: 93,
                aliases: ["data mile", "data miles"],
                targets: ["mi", "km"],
                tags: ["telecom"]),

            Override("shackle", isCommon: false, system: "Nautical", priority: 93,
                aliases: ["shackle", "shackles"],
                targets: ["ft", "m", "nmi"],
                tags: ["nautical"]),

            Override("dtp pica", isCommon: false, system: "Typographic", priority: 84,
                aliases: ["dtp pica", "dtp picas", "pica", "picas", "pica DTP", "css pc", "pc (CSS)"],
                targets: ["dtp pt", "px", "in", "mm"],
                tags: ["typography", "CSS"], symbol: "pica (DTP)") with
                { DisplayName = "DTP Pica", Description = "1/6 inch; 12 DTP points (CSS pica)" },

            Override("dtp pt", isCommon: false, system: "Typographic", priority: 83,
                aliases: ["dtp point", "dtp points", "dtp pt", "point", "points", "pt DTP", "css pt", "pt (CSS)"],
                targets: ["px", "dtp pica", "in", "mm"],
                tags: ["typography", "CSS"], symbol: "pt (DTP)") with
                { DisplayName = "DTP Point", Description = "1/72 inch (CSS point)" },

            Override("printer pica", isCommon: false, system: "Typographic", priority: 84,
                aliases: ["printer pica", "printer picas"],
                targets: ["printer pt", "in", "mm"],
                tags: ["typography", "traditional"], symbol: "pica (printer)") with
                { Description = "12 printer points; 400/2409 inch (TeX pica)" },

            Override("printer pt", isCommon: false, system: "Typographic", priority: 83,
                aliases: ["printer point", "printer points", "pt printer"],
                targets: ["printer pica", "in", "mm"],
                tags: ["typography", "traditional"], symbol: "pt (printer)") with
                { Description = "1/72.27 inch (TeX point)" },

            Override("twip", isCommon: false, system: "Typographic", priority: 85,
                aliases: ["twips"], targets: ["dtp pt", "px", "in"], tags: ["typography"]) with
                { Description = "1/20 DTP point; 1/1440 inch" },

            Override("au", isCommon: false, system: "Astronomical", priority: 95,
                aliases: ["astronomical unit", "astronomical units"],
                targets: ["km", "m", "ly", "pc"],
                tags: ["astronomical", "space"], symbol: "AU"),

            Override("ly", isCommon: false, system: "Astronomical", priority: 98,
                aliases: ["light year", "light years", "lightyear", "lightyears"],
                targets: ["km", "m", "pc", "au", "kly", "Mly"],
                tags: ["astronomical", "space"]),

            Override("kly", isCommon: false, system: "Astronomical", priority: 99,
                aliases: ["kilolightyear", "kilolightyears", "kilo light year", "kilo light years"],
                targets: ["ly", "Mly", "pc"],
                tags: ["astronomical", "space"]),

            Override("Mly", isCommon: false, system: "Astronomical", priority: 100,
                aliases: ["megalightyear", "megalightyears", "mega light year", "mega light years"],
                targets: ["kly", "ly", "Mpc"],
                tags: ["astronomical", "space"]),

            Override("pc", isCommon: false, system: "Astronomical", priority: 100,
                aliases: ["parsec", "parsecs"],
                targets: ["ly", "au", "km", "kpc", "Mpc"],
                tags: ["astronomical", "space"]),

            Override("kpc", isCommon: false, system: "Astronomical", priority: 101,
                aliases: ["kiloparsec", "kiloparsecs"],
                targets: ["pc", "Mpc"],
                tags: ["astronomical", "space"]),

            Override("Mpc", isCommon: false, system: "Astronomical", priority: 102,
                aliases: ["megaparsec", "megaparsecs"],
                targets: ["kpc", "pc"],
                tags: ["astronomical", "space"]),

            Override("angstrom", isCommon: false, system: "Non-SI", priority: 85,
                aliases: ["angstrom", "angstroms", "å"],
                targets: ["nm", "pm", "m"],
                tags: ["atomic", "physics"], symbol: "Å"),

            Override("fm", isCommon: false, system: "SI", priority: 90,
                aliases: ["femtometer", "femtometre", "femtometers", "femtometres", "fermi"],
                targets: ["pm", "nm", "m"],
                tags: ["SI", "atomic"]),

            Override("pm", isCommon: false, system: "SI", priority: 88,
                aliases: ["picometer", "picometre", "picometers", "picometres"],
                targets: ["nm", "angstrom", "m"],
                tags: ["SI", "atomic"]),

            Override("dm", isCommon: false, system: "SI", priority: 70,
                aliases: ["decimeter", "decimetre", "decimeters", "decimetres"],
                targets: ["cm", "m"],
                tags: ["SI"]),

            Override("dam", isCommon: false, system: "SI", priority: 75,
                aliases: ["decameter", "decametre", "dekameter", "dekametre", "decameters", "decametres"],
                targets: ["m", "hm"],
                tags: ["SI"]),

            Override("hm", isCommon: false, system: "SI", priority: 78,
                aliases: ["hectometer", "hectometre", "hectometers", "hectometres"],
                targets: ["dam", "km", "m"],
                tags: ["SI"]),

            Override("Mm", isCommon: false, system: "SI", priority: 92,
                aliases: ["megameter", "megametre", "megameters", "megametres"],
                targets: ["km", "Gm", "m"],
                tags: ["SI", "large"]),

            Override("Gm", isCommon: false, system: "SI", priority: 94,
                aliases: ["gigameter", "gigametre", "gigameters", "gigametres"],
                targets: ["Mm", "au", "m"],
                tags: ["SI", "large"]),
        ];
    }

    private static UnitDescriptor Override(
        string id,
        bool isCommon,
        string system,
        int priority,
        string[] aliases,
        string[] targets,
        string[] tags,
        string symbol = "")
    {
        return new UnitDescriptor
        {
            Id = id,
            Symbol = symbol,
            IsCommon = isCommon,
            System = system,
            SuggestionPriority = priority,
            Aliases = aliases,
            CommonTargets = targets,
            Tags = tags
        };
    }

    private static IReadOnlyCollection<UnitDescriptor> CreateCustomUnitDescriptors()
    {
        return
        [
            new UnitDescriptor
            {
                Id = "px",
                DisplayName = "CSS Pixel",
                Symbol = "px",
                System = "Typographic",
                IsCommon = false,
                Aliases = ["pixel", "pixels", "css pixel", "css pixels"],
                CommonTargets = ["dtp pt", "dtp pica", "mm", "in"],
                SuggestionPriority = 82,
                Tags = ["typography", "CSS"],
                Description = "1/96 inch (CSS reference pixel, independent of device pixel density)"
            },
            new UnitDescriptor
            {
                Id = "Q",
                DisplayName = "Quarter-millimeter",
                Symbol = "Q",
                System = "Typographic",
                IsCommon = false,
                Aliases = ["q", "quarter-millimeter", "quarter-millimeters", "quarter-millimetre", "quarter-millimetres"],
                CommonTargets = ["mm", "px", "dtp pt"],
                SuggestionPriority = 85,
                Tags = ["typography", "CSS"],
                Description = "1/4 millimeter (CSS quarter-millimeter)"
            },
            new UnitDescriptor
            {
                Id = "ls",
                DisplayName = "Light-second",
                Symbol = "ls",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["light second", "light seconds", "light-second", "light-seconds"],
                CommonTargets = ["km", "m", "ly"],
                SuggestionPriority = 96,
                Tags = ["astronomical", "relativity"],
                Description = "Distance light travels in one second"
            },
            new UnitDescriptor
            {
                Id = "lmin",
                DisplayName = "Light-minute",
                Symbol = "lmin",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["light minute", "light minutes", "light-minute", "light-minutes"],
                CommonTargets = ["km", "m", "ly"],
                SuggestionPriority = 96,
                Tags = ["astronomical", "relativity"],
                Description = "Distance light travels in one minute"
            },
            new UnitDescriptor
            {
                Id = "lh",
                DisplayName = "Light-hour",
                Symbol = "lh",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["light hour", "light hours", "light-hour", "light-hours"],
                CommonTargets = ["km", "m", "ly"],
                SuggestionPriority = 96,
                Tags = ["astronomical", "relativity"],
                Description = "Distance light travels in one hour"
            },
            new UnitDescriptor
            {
                Id = "ld",
                DisplayName = "Light-day",
                Symbol = "ld",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["light day", "light days", "light-day", "light-days"],
                CommonTargets = ["km", "m", "ly"],
                SuggestionPriority = 97,
                Tags = ["astronomical", "relativity"],
                Description = "Distance light travels in one day"
            },
            new UnitDescriptor
            {
                Id = "lw",
                DisplayName = "Light-week",
                Symbol = "lw",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["light week", "light weeks", "light-week", "light-weeks"],
                CommonTargets = ["km", "m", "ly"],
                SuggestionPriority = 97,
                Tags = ["astronomical", "relativity"],
                Description = "Distance light travels in one week"
            }
        ];
    }

    public override bool TryNormalizeUnit(string unit, CultureInfo culture, out string normalizedUnit)
    {
        unit = ConverterText.NormalizeUnit(Category, unit, culture);
        if (TryGetAmbiguity(unit, out _, out _))
        {
            normalizedUnit = string.Empty;
            return false;
        }

        var collection = GetUnits(culture);
        return IsCaseSensitive
            ? collection.TryNormalize(unit, out normalizedUnit, out _)
            : collection.TryNormalizeCaseInsensitive(unit, out normalizedUnit, out _);
    }

    public override UnitMetadata? GetUnitMetadata(string unit, CultureInfo? culture = null)
    {
        var lookupCulture = culture ?? CultureInfo.CurrentUICulture;
        var collection = GetUnits(lookupCulture);
        if (!TryNormalizeUnit(unit, lookupCulture, out var normalized))
            return null;

        return ConverterText.Localize(Category, normalized, collection.GetById(normalized)?.ToMetadata(), lookupCulture);
    }

    public override IReadOnlyDictionary<string, UnitMetadata> GetAllUnitMetadata(CultureInfo? culture = null)
    {
        var lookupCulture = culture ?? CultureInfo.CurrentUICulture;
        var collection = GetUnits(lookupCulture);
        return collection.GetAll().ToDictionary(unit => unit.Id,
            unit => ConverterText.Localize(Category, unit.Id, unit.ToMetadata(), lookupCulture)!, StringComparer.Ordinal);
    }

    private UnitDescriptorCatalog GetUnits(CultureInfo? culture)
    {
        var lookupCulture = culture ?? CultureInfo.InvariantCulture;
        var key = lookupCulture.Name.Length == 0 ? CultureInfo.InvariantCulture.Name : lookupCulture.Name;

        if (_localizedCollections.TryGetValue(key, out var collection))
            return collection;

        var descriptors = UnitsNetDescriptorProvider.CreateDescriptors(UnitType.Length, lookupCulture, _descriptorOptions, _customDescriptors);
        collection = new UnitDescriptorCatalog(descriptors);
        _localizedCollections[key] = collection;
        return collection;
    }

    private static Length CreateLengthDirectly(double value, string unit)
    {
        return unit switch
        {
            "m" => Length.FromMeters(value),
            "cm" => Length.FromCentimeters(value),
            "mm" => Length.FromMillimeters(value),
            "km" => Length.FromKilometers(value),
            "ft" => Length.FromFeet(value),
            "in" => Length.FromInches(value),
            "yd" => Length.FromYards(value),
            "mi" => Length.FromMiles(value),
            "nmi" => Length.FromNauticalMiles(value),
            // Survey
            "us survey ft" => Length.FromUsSurveyFeet(value),
            // Other customary
            "ch" => Length.FromChains(value),
            "fathom" => Length.FromFathoms(value),
            "hand" => Length.FromHands(value),
            "mil" => Length.FromMils(value),
            "microinch" => Length.FromMicroinches(value),
            "kft" => Length.FromKilofeet(value),
            "kyd" => Length.FromKiloyards(value),
            "dmi" => Length.FromDataMiles(value),
            "shackle" => Length.FromShackles(value),
            // Typographic
            "dtp pica" => Length.FromDtpPicas(value),
            "dtp pt" => Length.FromDtpPoints(value),
            // Avoid the rounded printer-pica factor in Units.NET 5.x.
            "printer pica" => Length.FromPrinterPoints(value * 12),
            "printer pt" => Length.FromPrinterPoints(value),
            "twip" => Length.FromTwips(value),
            "px" => Length.FromInches(value / 96),
            "Q" => Length.FromMillimeters(value / 4),
            // Astronomical (built-in and scaled)
            "au" => Length.FromAstronomicalUnits(value),
            "ly" => Length.FromLightYears(value),
            "kly" => Length.FromKilolightYears(value),
            "Mly" => Length.FromMegalightYears(value),
            "pc" => Length.FromParsecs(value),
            "kpc" => Length.FromKiloparsecs(value),
            "Mpc" => Length.FromMegaparsecs(value),
            "R⊙" => Length.FromSolarRadiuses(value),
            // Small-scale SI and atomic scale
            "angstrom" => Length.FromAngstroms(value),
            "fm" => Length.FromFemtometers(value),
            "pm" => Length.FromPicometers(value),
            "nm" => Length.FromNanometers(value),
            "um" => Length.FromMicrometers(value),
            "dm" => Length.FromDecimeters(value),
            "dam" => Length.FromDecameters(value),
            "hm" => Length.FromHectometers(value),
            "Mm" => Length.FromMegameters(value),
            "Gm" => Length.FromGigameters(value),
            // Light-based custom
            "ls" => Length.FromMeters(value * SpeedOfLight),
            "lmin" => Length.FromMeters(value * SpeedOfLight * SecondsPerMinute),
            "lh" => Length.FromMeters(value * SpeedOfLight * SecondsPerHour),
            "ld" => Length.FromMeters(value * SpeedOfLight * SecondsPerDay),
            "lw" => Length.FromMeters(value * SpeedOfLight * SecondsPerWeek),
            _ => throw new ArgumentException($"Unsupported unit: {unit}")
        };
    }

    private static double? ConvertToUnit(Length length, string unit)
    {
        return unit switch
        {
            "m" => length.Meters,
            "cm" => length.Centimeters,
            "mm" => length.Millimeters,
            "km" => length.Kilometers,
            "ft" => length.Feet,
            "in" => length.Inches,
            "yd" => length.Yards,
            "mi" => length.Miles,
            "nmi" => length.NauticalMiles,
            // Survey
            "us survey ft" => length.UsSurveyFeet,
            // Other customary
            "ch" => length.Chains,
            "fathom" => length.Fathoms,
            "hand" => length.Hands,
            "mil" => length.Mils,
            "microinch" => length.Microinches,
            "kft" => length.Kilofeet,
            "kyd" => length.Kiloyards,
            "dmi" => length.DataMiles,
            "shackle" => length.Shackles,
            // Typographic
            "dtp pica" => length.DtpPicas,
            "dtp pt" => length.DtpPoints,
            "printer pica" => length.PrinterPoints / 12,
            "printer pt" => length.PrinterPoints,
            "twip" => length.Twips,
            "px" => length.Inches * 96,
            "Q" => length.Millimeters * 4,
            // Astronomical (built-in and scaled)
            "au" => length.AstronomicalUnits,
            "ly" => length.LightYears,
            "kly" => length.KilolightYears,
            "Mly" => length.MegalightYears,
            "pc" => length.Parsecs,
            "kpc" => length.Kiloparsecs,
            "Mpc" => length.Megaparsecs,
            "R⊙" => length.SolarRadiuses,
            // Small-scale SI and atomic scale
            "angstrom" => length.Angstroms,
            "fm" => length.Femtometers,
            "pm" => length.Picometers,
            "nm" => length.Nanometers,
            "um" => length.Micrometers,
            "dm" => length.Decimeters,
            "dam" => length.Decameters,
            "hm" => length.Hectometers,
            "Mm" => length.Megameters,
            "Gm" => length.Gigameters,
            // Light-based custom
            "ls" => length.Meters / SpeedOfLight,
            "lmin" => length.Meters / (SpeedOfLight * SecondsPerMinute),
            "lh" => length.Meters / (SpeedOfLight * SecondsPerHour),
            "ld" => length.Meters / (SpeedOfLight * SecondsPerDay),
            "lw" => length.Meters / (SpeedOfLight * SecondsPerWeek),
            _ => null
        };
    }
}