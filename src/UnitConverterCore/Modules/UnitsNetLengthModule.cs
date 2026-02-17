// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
//
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Length conversion module powered by Units.NET with support for squared and cubic units using the UnitDescriptor system
/// </summary>
public class UnitsNetLengthModule : DescriptorBasedConversionModule
{
    public UnitsNetLengthModule() : base(CreateUnitDescriptors())
    {
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

            Length length;
            try
            {
                length = Length.Parse($"{value} {from}");
            }
            catch
            {
                length = CreateLengthDirectly(value, from);
            }

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

    private static UnitDescriptor[] CreateUnitDescriptors()
    {
        return
        [
            new UnitDescriptor
            {
                Id = "mm",
                DisplayName = "Millimeter",
                Symbol = "mm",
                System = "Metric",
                IsCommon = true,
                Aliases = ["millimeter", "millimetre", "millimeters", "millimetres"],
                CommonTargets = ["cm", "m", "in"],
                SuggestionPriority = 20,
                Tags = ["SI", "metric", "small", "precision"],
                Description = "One thousandth of a meter"
            },
            new UnitDescriptor
            {
                Id = "cm",
                DisplayName = "Centimeter",
                Symbol = "cm",
                System = "Metric",
                IsCommon = true,
                Aliases = ["centimeter", "centimetre", "centimeters", "centimetres"],
                CommonTargets = ["mm", "m", "in", "ft"],
                SuggestionPriority = 15,
                Tags = ["SI", "metric", "common"],
                Description = "One hundredth of a meter"
            },
            new UnitDescriptor
            {
                Id = "m",
                DisplayName = "Meter",
                Symbol = "m",
                System = "Metric",
                IsCommon = true,
                Aliases = ["meter", "metre", "meters", "metres"],
                CommonTargets = ["cm", "ft", "yd", "km"],
                SuggestionPriority = 5,
                Tags = ["SI", "base", "metric", "common"],
                Description = "SI base unit of length"
            },
            new UnitDescriptor
            {
                Id = "km",
                DisplayName = "Kilometer",
                Symbol = "km",
                System = "Metric",
                IsCommon = true,
                Aliases = ["kilometer", "kilometre", "kilometers", "kilometres", "klick", "klicks", "click", "clicks"],
                CommonTargets = ["m", "mi"],
                SuggestionPriority = 25,
                Tags = ["SI", "metric", "distance"],
                Description = "One thousand meters"
            },
            new UnitDescriptor
            {
                Id = "in",
                DisplayName = "Inch",
                Symbol = "in",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["inch", "inches", "\""],
                CommonTargets = ["cm", "mm", "ft"],
                SuggestionPriority = 10,
                Tags = ["imperial", "common", "US"],
                Description = "1/12 of a foot"
            },
            new UnitDescriptor
            {
                Id = "ft",
                DisplayName = "Foot",
                Symbol = "ft",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["foot", "feet", "'"],
                CommonTargets = ["m", "cm", "in", "yd"],
                SuggestionPriority = 8,
                Tags = ["imperial", "common", "US"],
                Description = "12 inches"
            },
            new UnitDescriptor
            {
                Id = "us survey ft",
                DisplayName = "US Survey Foot",
                Symbol = "US ft (survey)",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["us survey foot", "us survey feet", "us ft (survey)", "survey foot", "survey feet"],
                CommonTargets = ["ft", "m"],
                SuggestionPriority = 82,
                Tags = ["imperial", "US", "survey"],
                Description = "US survey foot"
            },
            new UnitDescriptor
            {
                Id = "yd",
                DisplayName = "Yard",
                Symbol = "yd",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["yard", "yards"],
                CommonTargets = ["m", "ft"],
                SuggestionPriority = 30,
                Tags = ["imperial", "US"],
                Description = "3 feet"
            },
            new UnitDescriptor
            {
                Id = "mi",
                DisplayName = "Mile",
                Symbol = "mi",
                System = "Imperial",
                IsCommon = true,
                Aliases = ["mile", "miles"],
                CommonTargets = ["km", "m"],
                SuggestionPriority = 35,
                Tags = ["imperial", "distance", "US"],
                Description = "5,280 feet"
            },
            new UnitDescriptor
            {
                Id = "nmi",
                DisplayName = "Nautical Mile",
                Symbol = "nmi",
                System = "Nautical",
                IsCommon = false,
                Aliases = ["nautical mile", "nautical miles"],
                CommonTargets = ["km", "mi"],
                SuggestionPriority = 80,
                Tags = ["nautical", "maritime", "aviation"],
                Description = "1,852 meters (international)"
            },
            // Other customary units
            new UnitDescriptor
            {
                Id = "ch",
                DisplayName = "Chain",
                Symbol = "ch",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["chain", "chains"],
                CommonTargets = ["m", "ft", "yd"],
                SuggestionPriority = 85,
                Tags = ["imperial", "survey"],
                Description = "66 feet"
            },
            new UnitDescriptor
            {
                Id = "fathom",
                DisplayName = "Fathom",
                Symbol = "ftm",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["fathom", "fathoms"],
                CommonTargets = ["m", "ft", "yd"],
                SuggestionPriority = 86,
                Tags = ["imperial", "nautical"],
                Description = "6 feet"
            },
            new UnitDescriptor
            {
                Id = "hand",
                DisplayName = "Hand",
                Symbol = "hand",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["hand", "hands"],
                CommonTargets = ["in", "ft", "cm"],
                SuggestionPriority = 87,
                Tags = ["imperial", "equine"],
                Description = "4 inches"
            },
            new UnitDescriptor
            {
                Id = "mil",
                DisplayName = "Mil",
                Symbol = "mil",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["mil", "thou"],
                CommonTargets = ["in", "mm"],
                SuggestionPriority = 88,
                Tags = ["imperial", "manufacturing"],
                Description = "0.001 inch"
            },
            new UnitDescriptor
            {
                Id = "microinch",
                DisplayName = "Microinch",
                Symbol = "µin",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["microinch", "microinches", "µin", "uin"],
                CommonTargets = ["mil", "in", "mm"],
                SuggestionPriority = 89,
                Tags = ["imperial", "precision"],
                Description = "1e-6 inches"
            },
            new UnitDescriptor
            {
                Id = "kft",
                DisplayName = "Kilofeet",
                Symbol = "kft",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["kilofeet", "kilofoot"],
                CommonTargets = ["ft", "mi"],
                SuggestionPriority = 91,
                Tags = ["imperial"],
                Description = "1000 feet"
            },
            new UnitDescriptor
            {
                Id = "kyd",
                DisplayName = "Kiloyard",
                Symbol = "kyd",
                System = "Imperial",
                IsCommon = false,
                Aliases = ["kiloyard", "kiloyards"],
                CommonTargets = ["yd", "mi"],
                SuggestionPriority = 91,
                Tags = ["imperial"],
                Description = "1000 yards"
            },
            new UnitDescriptor
            {
                Id = "dmi",
                DisplayName = "Data Mile",
                Symbol = "dmi",
                System = "Customary",
                IsCommon = false,
                Aliases = ["data mile", "data miles"],
                CommonTargets = ["mi", "km"],
                SuggestionPriority = 93,
                Tags = ["telecom"],
                Description = "6000 feet"
            },
            new UnitDescriptor
            {
                Id = "shackle",
                DisplayName = "Shackle",
                Symbol = "shackle",
                System = "Nautical",
                IsCommon = false,
                Aliases = ["shackle", "shackles"],
                CommonTargets = ["ft", "m", "nmi"],
                SuggestionPriority = 93,
                Tags = ["nautical"],
                Description = "15 fathoms (90 feet)"
            },
            // Typographic units
            new UnitDescriptor
            {
                Id = "dtp pica",
                DisplayName = "DTP Pica",
                Symbol = "pica (DTP)",
                System = "Typographic",
                IsCommon = false,
                Aliases = ["dtp pica", "pica"],
                CommonTargets = ["dtp pt", "in", "mm"],
                SuggestionPriority = 84,
                Tags = ["typography"],
                Description = "1/6 of an inch (PostScript)"
            },
            new UnitDescriptor
            {
                Id = "dtp pt",
                DisplayName = "DTP Point",
                Symbol = "pt (DTP)",
                System = "Typographic",
                IsCommon = false,
                Aliases = ["dtp point", "dtp pt", "point"],
                CommonTargets = ["dtp pica", "in", "mm"],
                SuggestionPriority = 83,
                Tags = ["typography"],
                Description = "1/72 of an inch (PostScript)"
            },
            new UnitDescriptor
            {
                Id = "printer pica",
                DisplayName = "Printer Pica",
                Symbol = "pica (printer)",
                System = "Typographic",
                IsCommon = false,
                Aliases = ["printer pica"],
                CommonTargets = ["printer pt", "in", "mm"],
                SuggestionPriority = 84,
                Tags = ["typography", "traditional"],
                Description = "Traditional printer's pica"
            },
            new UnitDescriptor
            {
                Id = "printer pt",
                DisplayName = "Printer Point",
                Symbol = "pt (printer)",
                System = "Typographic",
                IsCommon = false,
                Aliases = ["printer point"],
                CommonTargets = ["printer pica", "in", "mm"],
                SuggestionPriority = 83,
                Tags = ["typography", "traditional"],
                Description = "Traditional printer's point"
            },
            // Astronomical units (built-in and scaled)
            new UnitDescriptor
            {
                Id = "au",
                DisplayName = "Astronomical Unit",
                Symbol = "AU",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["astronomical unit", "astronomical units"],
                CommonTargets = ["km", "m", "ly", "pc"],
                SuggestionPriority = 95,
                Tags = ["astronomical", "space"],
                Description = "Average Earth–Sun distance (~149,597,870.7 km)"
            },
            new UnitDescriptor
            {
                Id = "ly",
                DisplayName = "Light-year",
                Symbol = "ly",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["light year", "light years", "lightyear", "lightyears"],
                CommonTargets = ["km", "m", "pc", "au", "kly", "Mly"],
                SuggestionPriority = 98,
                Tags = ["astronomical", "space"],
                Description = "Distance light travels in one Julian year"
            },
            new UnitDescriptor
            {
                Id = "kly",
                DisplayName = "Kilolight-year",
                Symbol = "kly",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["kilolightyear", "kilolightyears", "kilo light year", "kilo light years"],
                CommonTargets = ["ly", "Mly", "pc"],
                SuggestionPriority = 99,
                Tags = ["astronomical", "space"],
                Description = "1,000 light-years"
            },
            new UnitDescriptor
            {
                Id = "Mly",
                DisplayName = "Megalight-year",
                Symbol = "Mly",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["megalightyear", "megalightyears", "mega light year", "mega light years"],
                CommonTargets = ["kly", "ly", "Mpc"],
                SuggestionPriority = 100,
                Tags = ["astronomical", "space"],
                Description = "1,000,000 light-years"
            },
            new UnitDescriptor
            {
                Id = "pc",
                DisplayName = "Parsec",
                Symbol = "pc",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["parsec", "parsecs"],
                CommonTargets = ["ly", "au", "km", "kpc", "Mpc"],
                SuggestionPriority = 100,
                Tags = ["astronomical", "space"],
                Description = "Approximately 3.26 light-years"
            },
            new UnitDescriptor
            {
                Id = "kpc",
                DisplayName = "Kiloparsec",
                Symbol = "kpc",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["kiloparsec", "kiloparsecs"],
                CommonTargets = ["pc", "Mpc"],
                SuggestionPriority = 101,
                Tags = ["astronomical", "space"],
                Description = "1,000 parsecs"
            },
            new UnitDescriptor
            {
                Id = "Mpc",
                DisplayName = "Megaparsec",
                Symbol = "Mpc",
                System = "Astronomical",
                IsCommon = false,
                Aliases = ["megaparsec", "megaparsecs"],
                CommonTargets = ["kpc", "pc"],
                SuggestionPriority = 102,
                Tags = ["astronomical", "space"],
                Description = "1,000,000 parsecs"
            },
            // Small-scale SI and atomic scale
            new UnitDescriptor
            {
                Id = "angstrom",
                DisplayName = "Angstrom",
                Symbol = "Å",
                System = "Non-SI",
                IsCommon = false,
                Aliases = ["angstrom", "angstroms", "å"],
                CommonTargets = ["nm", "pm", "m"],
                SuggestionPriority = 85,
                Tags = ["atomic", "physics"],
                Description = "1 Å = 1e-10 m"
            },
            new UnitDescriptor
            {
                Id = "fm",
                DisplayName = "Femtometer",
                Symbol = "fm",
                System = "SI",
                IsCommon = false,
                Aliases = ["femtometer", "femtometre", "femtometers", "femtometres", "fermi"],
                CommonTargets = ["pm", "nm", "m"],
                SuggestionPriority = 90,
                Tags = ["SI", "atomic"],
                Description = "1e-15 meters"
            },
            new UnitDescriptor
            {
                Id = "pm",
                DisplayName = "Picometer",
                Symbol = "pm",
                System = "SI",
                IsCommon = false,
                Aliases = ["picometer", "picometre", "picometers", "picometres"],
                CommonTargets = ["nm", "angstrom", "m"],
                SuggestionPriority = 88,
                Tags = ["SI", "atomic"],
                Description = "1e-12 meters"
            },
            new UnitDescriptor
            {
                Id = "nm",
                DisplayName = "Nanometer",
                Symbol = "nm",
                System = "SI",
                IsCommon = true,
                Aliases = ["nanometer", "nanometre", "nanometers", "nanometres"],
                CommonTargets = ["um", "m", "angstrom"],
                SuggestionPriority = 40,
                Tags = ["SI", "science"],
                Description = "1e-9 meters"
            },
            new UnitDescriptor
            {
                Id = "um",
                DisplayName = "Micrometer",
                Symbol = "µm",
                System = "SI",
                IsCommon = true,
                Aliases = ["micrometer", "micrometre", "micrometers", "micrometres", "um", "µm"],
                CommonTargets = ["mm", "nm", "m"],
                SuggestionPriority = 45,
                Tags = ["SI", "science"],
                Description = "1e-6 meters"
            },
            new UnitDescriptor
            {
                Id = "dm",
                DisplayName = "Decimeter",
                Symbol = "dm",
                System = "SI",
                IsCommon = false,
                Aliases = ["decimeter", "decimetre", "decimeters", "decimetres"],
                CommonTargets = ["cm", "m"],
                SuggestionPriority = 70,
                Tags = ["SI"],
                Description = "0.1 meters"
            },
            new UnitDescriptor
            {
                Id = "dam",
                DisplayName = "Decameter",
                Symbol = "dam",
                System = "SI",
                IsCommon = false,
                Aliases = ["decameter", "decametre", "dekameter", "dekametre", "decameters", "decametres"],
                CommonTargets = ["m", "hm"],
                SuggestionPriority = 75,
                Tags = ["SI"],
                Description = "10 meters"
            },
            new UnitDescriptor
            {
                Id = "hm",
                DisplayName = "Hectometer",
                Symbol = "hm",
                System = "SI",
                IsCommon = false,
                Aliases = ["hectometer", "hectometre", "hectometers", "hectometres"],
                CommonTargets = ["dam", "km", "m"],
                SuggestionPriority = 78,
                Tags = ["SI"],
                Description = "100 meters"
            },
            new UnitDescriptor
            {
                Id = "Mm",
                DisplayName = "Megameter",
                Symbol = "Mm",
                System = "SI",
                IsCommon = false,
                Aliases = ["megameter", "megametre", "megameters", "megametres"],
                CommonTargets = ["km", "Gm", "m"],
                SuggestionPriority = 92,
                Tags = ["SI", "large"],
                Description = "1e6 meters"
            },
            new UnitDescriptor
            {
                Id = "Gm",
                DisplayName = "Gigameter",
                Symbol = "Gm",
                System = "SI",
                IsCommon = false,
                Aliases = ["gigameter", "gigametre", "gigameters", "gigametres"],
                CommonTargets = ["Mm", "au", "m"],
                SuggestionPriority = 94,
                Tags = ["SI", "large"],
                Description = "1e9 meters"
            },
            // Light-based (custom) units
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

    private static Length CreateLengthDirectly(double value, string unit)
    {
        return unit.ToLowerInvariant() switch
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
            "printer pica" => Length.FromPrinterPicas(value),
            "printer pt" => Length.FromPrinterPoints(value),
            // Astronomical (built-in and scaled)
            "au" => Length.FromAstronomicalUnits(value),
            "ly" => Length.FromLightYears(value),
            "kly" => Length.FromKilolightYears(value),
            "Mly" => Length.FromMegalightYears(value),
            "pc" => Length.FromParsecs(value),
            "kpc" => Length.FromKiloparsecs(value),
            "Mpc" => Length.FromMegaparsecs(value),
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
        return unit.ToLowerInvariant() switch
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
            "printer pica" => length.PrinterPicas,
            "printer pt" => length.PrinterPoints,
            // Astronomical (built-in and scaled)
            "au" => length.AstronomicalUnits,
            "ly" => length.LightYears,
            "kly" => length.KilolightYears,
            "Mly" => length.MegalightYears,
            "pc" => length.Parsecs,
            "kpc" => length.Kiloparsecs,
            "Mpc" => length.Megaparsecs,
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