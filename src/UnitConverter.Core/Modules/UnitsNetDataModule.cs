// ------------------------------------------------------------
//
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Data/Information conversion module with SI and IEC prefixes, bits and bytes using the UnitDescriptor system
/// </summary>
internal class UnitsNetDataModule : DescriptorBasedConversionModule
{
    public UnitsNetDataModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Data";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    // Preserve case-sensitivity: 'b' vs 'B', 'kb' vs 'kB'
    public override bool IsCaseSensitive => true;

    protected override UnitType GetUnitType() => UnitType.Data;

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

            var fromBits = ToBitsFactor(from);
            var toBits = ToBitsFactor(to);
            if (fromBits == 0 || toBits == 0) return false;

            var bits = value * fromBits;
            result = bits / toBits;
            return true;
        }
        catch { }
        return false;
    }

    private static UnitDescriptor[] CreateUnitDescriptors()
    {
        return
        [
            new UnitDescriptor
            {
                Id = "b",
                DisplayName = "Bit",
                Symbol = "b",
                System = "Digital",
                IsCommon = true,
                Aliases = ["bit", "bits"],
                CommonTargets = ["B", "kb", "kB"],
                SuggestionPriority = 20,
                Tags = ["digital", "binary", "smallest"],
                Description = "Basic unit of information"
            },
            new UnitDescriptor
            {
                Id = "nibble",
                DisplayName = "Nibble",
                Symbol = "nibble",
                System = "Digital",
                IsCommon = false,
                Aliases = ["nybble", "nibbles", "nybbles"],
                CommonTargets = ["b", "B"],
                SuggestionPriority = 90,
                Tags = ["digital", "binary", "small"],
                Description = "4 bits (half a byte)"
            },
            new UnitDescriptor
            {
                Id = "B",
                DisplayName = "Byte",
                Symbol = "B",
                System = "Digital",
                IsCommon = true,
                Aliases = ["byte", "bytes", "octet", "octets"],
                CommonTargets = ["kB", "MB", "KiB"],
                SuggestionPriority = 10,
                Tags = ["digital", "common"],
                Description = "8 bits"
            },
            
            // Decimal bit units (SI prefixes)
            new UnitDescriptor
            {
                Id = "kb",
                DisplayName = "Kilobit",
                Symbol = "kb",
                System = "Digital",
                IsCommon = true,
                Aliases = ["kilobit", "kilobits", "kbit", "kbits"],
                CommonTargets = ["b", "Mb", "kB"],
                SuggestionPriority = 25,
                Tags = ["digital", "decimal", "SI", "bits"],
                Description = "1000 bits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Mb",
                DisplayName = "Megabit",
                Symbol = "Mb",
                System = "Digital",
                IsCommon = true,
                Aliases = ["megabit", "megabits", "Mbit", "Mbits"],
                CommonTargets = ["kb", "Gb", "MB"],
                SuggestionPriority = 28,
                Tags = ["digital", "decimal", "SI", "bits"],
                Description = "1000 kilobits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Gb",
                DisplayName = "Gigabit",
                Symbol = "Gb",
                System = "Digital",
                IsCommon = true,
                Aliases = ["gigabit", "gigabits", "Gbit", "Gbits"],
                CommonTargets = ["Mb", "Tb", "GB"],
                SuggestionPriority = 32,
                Tags = ["digital", "decimal", "SI", "bits"],
                Description = "1000 megabits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Tb",
                DisplayName = "Terabit",
                Symbol = "Tb",
                System = "Digital",
                IsCommon = false,
                Aliases = ["terabit", "terabits", "Tbit", "Tbits"],
                CommonTargets = ["Gb", "Pb", "TB"],
                SuggestionPriority = 35,
                Tags = ["digital", "decimal", "SI", "bits", "large"],
                Description = "1000 gigabits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Pb",
                DisplayName = "Petabit",
                Symbol = "Pb",
                System = "Digital",
                IsCommon = false,
                Aliases = ["petabit", "petabits", "Pbit", "Pbits"],
                CommonTargets = ["Tb", "Eb", "PB"],
                SuggestionPriority = 40,
                Tags = ["digital", "decimal", "SI", "bits", "very-large"],
                Description = "1000 terabits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Eb",
                DisplayName = "Exabit",
                Symbol = "Eb",
                System = "Digital",
                IsCommon = false,
                Aliases = ["exabit", "exabits", "Ebit", "Ebits"],
                CommonTargets = ["Pb", "Zb", "EB"],
                SuggestionPriority = 45,
                Tags = ["digital", "decimal", "SI", "bits", "very-large"],
                Description = "1000 petabits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Zb",
                DisplayName = "Zettabit",
                Symbol = "Zb",
                System = "Digital",
                IsCommon = false,
                Aliases = ["zettabit", "zettabits", "Zbit", "Zbits"],
                CommonTargets = ["Eb", "Yb", "ZB"],
                SuggestionPriority = 50,
                Tags = ["digital", "decimal", "SI", "bits", "massive"],
                Description = "1000 exabits (decimal)"
            },
            new UnitDescriptor
            {
                Id = "Yb",
                DisplayName = "Yottabit",
                Symbol = "Yb",
                System = "Digital",
                IsCommon = false,
                Aliases = ["yottabit", "yottabits", "Ybit", "Ybits"],
                CommonTargets = ["Zb", "YB"],
                SuggestionPriority = 55,
                Tags = ["digital", "decimal", "SI", "bits", "massive"],
                Description = "1000 zettabits (decimal)"
            },
            
            // Binary bit units (IEC prefixes)
            new UnitDescriptor
            {
                Id = "Kib",
                DisplayName = "Kibibit",
                Symbol = "Kib",
                System = "Digital",
                IsCommon = false,
                Aliases = ["kibibit", "kibibits"],
                CommonTargets = ["b", "Mib", "KiB"],
                SuggestionPriority = 60,
                Tags = ["digital", "binary", "IEC", "bits"],
                Description = "1024 bits (binary)"
            },
            new UnitDescriptor
            {
                Id = "Mib",
                DisplayName = "Mebibit",
                Symbol = "Mib",
                System = "Digital",
                IsCommon = false,
                Aliases = ["mebibit", "mebibits"],
                CommonTargets = ["Kib", "Gib", "MiB"],
                SuggestionPriority = 65,
                Tags = ["digital", "binary", "IEC", "bits"],
                Description = "1024 kibibits (binary)"
            },
            new UnitDescriptor
            {
                Id = "Gib",
                DisplayName = "Gibibit",
                Symbol = "Gib",
                System = "Digital",
                IsCommon = false,
                Aliases = ["gibibit", "gibibits"],
                CommonTargets = ["Mib", "Tib", "GiB"],
                SuggestionPriority = 70,
                Tags = ["digital", "binary", "IEC", "bits"],
                Description = "1024 mebibits (binary)"
            },
            new UnitDescriptor
            {
                Id = "Tib",
                DisplayName = "Tebibit",
                Symbol = "Tib",
                System = "Digital",
                IsCommon = false,
                Aliases = ["tebibit", "tebibits"],
                CommonTargets = ["Gib", "Pib", "TiB"],
                SuggestionPriority = 75,
                Tags = ["digital", "binary", "IEC", "bits"],
                Description = "1024 gibibits (binary)"
            },
            new UnitDescriptor
            {
                Id = "Pib",
                DisplayName = "Pebibit",
                Symbol = "Pib",
                System = "Digital",
                IsCommon = false,
                Aliases = ["pebibit", "pebibits"],
                CommonTargets = ["Tib", "Eib", "PiB"],
                SuggestionPriority = 80,
                Tags = ["digital", "binary", "IEC", "bits"],
                Description = "1024 tebibits (binary)"
            },
            new UnitDescriptor
            {
                Id = "Eib",
                DisplayName = "Exbibit",
                Symbol = "Eib",
                System = "Digital",
                IsCommon = false,
                Aliases = ["exbibit", "exbibits"],
                CommonTargets = ["Pib", "Zib", "EiB"],
                SuggestionPriority = 85,
                Tags = ["digital", "binary", "IEC", "bits"],
                Description = "1024 pebibits (binary)"
            },
            
            // Decimal byte units (SI prefixes)
            new UnitDescriptor
            {
                Id = "kB",
                DisplayName = "Kilobyte",
                Symbol = "kB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["kilobyte", "kilobytes"],
                CommonTargets = ["B", "MB", "KiB"],
                SuggestionPriority = 5,
                Tags = ["digital", "decimal", "SI", "common", "bytes"],
                Description = "1000 bytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "MB",
                DisplayName = "Megabyte",
                Symbol = "MB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["megabyte", "megabytes"],
                CommonTargets = ["kB", "GB", "MiB"],
                SuggestionPriority = 8,
                Tags = ["digital", "decimal", "SI", "common", "bytes"],
                Description = "1000 kilobytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "GB",
                DisplayName = "Gigabyte",
                Symbol = "GB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["gigabyte", "gigabytes"],
                CommonTargets = ["MB", "TB", "GiB"],
                SuggestionPriority = 12,
                Tags = ["digital", "decimal", "SI", "common", "bytes"],
                Description = "1000 megabytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "TB",
                DisplayName = "Terabyte",
                Symbol = "TB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["terabyte", "terabytes"],
                CommonTargets = ["GB", "PB", "TiB"],
                SuggestionPriority = 15,
                Tags = ["digital", "decimal", "SI", "large", "bytes"],
                Description = "1000 gigabytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "PB",
                DisplayName = "Petabyte",
                Symbol = "PB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["petabyte", "petabytes"],
                CommonTargets = ["TB", "EB", "PiB"],
                SuggestionPriority = 20,
                Tags = ["digital", "decimal", "SI", "very-large", "bytes"],
                Description = "1000 terabytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "EB",
                DisplayName = "Exabyte",
                Symbol = "EB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["exabyte", "exabytes"],
                CommonTargets = ["PB", "ZB", "EiB"],
                SuggestionPriority = 25,
                Tags = ["digital", "decimal", "SI", "very-large", "bytes"],
                Description = "1000 petabytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "ZB",
                DisplayName = "Zettabyte",
                Symbol = "ZB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["zettabyte", "zettabytes"],
                CommonTargets = ["EB", "YB", "ZiB"],
                SuggestionPriority = 30,
                Tags = ["digital", "decimal", "SI", "massive", "bytes"],
                Description = "1000 exabytes (decimal)"
            },
            new UnitDescriptor
            {
                Id = "YB",
                DisplayName = "Yottabyte",
                Symbol = "YB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["yottabyte", "yottabytes"],
                CommonTargets = ["ZB", "YiB"],
                SuggestionPriority = 35,
                Tags = ["digital", "decimal", "SI", "massive", "bytes"],
                Description = "1000 zettabytes (decimal)"
            },
            
            // Binary byte units (IEC prefixes)
            new UnitDescriptor
            {
                Id = "KiB",
                DisplayName = "Kibibyte",
                Symbol = "KiB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["kibibyte", "kibibytes"],
                CommonTargets = ["B", "MiB", "kB"],
                SuggestionPriority = 18,
                Tags = ["digital", "binary", "IEC", "bytes"],
                Description = "1024 bytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "MiB",
                DisplayName = "Mebibyte",
                Symbol = "MiB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["mebibyte", "mebibytes"],
                CommonTargets = ["KiB", "GiB", "MB"],
                SuggestionPriority = 22,
                Tags = ["digital", "binary", "IEC", "bytes"],
                Description = "1024 kibibytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "GiB",
                DisplayName = "Gibibyte",
                Symbol = "GiB",
                System = "Digital",
                IsCommon = true,
                Aliases = ["gibibyte", "gibibytes"],
                CommonTargets = ["MiB", "TiB", "GB"],
                SuggestionPriority = 25,
                Tags = ["digital", "binary", "IEC", "bytes"],
                Description = "1024 mebibytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "TiB",
                DisplayName = "Tebibyte",
                Symbol = "TiB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["tebibyte", "tebibytes"],
                CommonTargets = ["GiB", "PiB", "TB"],
                SuggestionPriority = 30,
                Tags = ["digital", "binary", "IEC", "large", "bytes"],
                Description = "1024 gibibytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "PiB",
                DisplayName = "Pebibyte",
                Symbol = "PiB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["pebibyte", "pebibytes"],
                CommonTargets = ["TiB", "EiB", "PB"],
                SuggestionPriority = 40,
                Tags = ["digital", "binary", "IEC", "very-large", "bytes"],
                Description = "1024 tebibytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "EiB",
                DisplayName = "Exbibyte",
                Symbol = "EiB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["exbibyte", "exbibytes"],
                CommonTargets = ["PiB", "ZiB", "EB"],
                SuggestionPriority = 45,
                Tags = ["digital", "binary", "IEC", "very-large", "bytes"],
                Description = "1024 pebibytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "ZiB",
                DisplayName = "Zebibyte",
                Symbol = "ZiB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["zebibyte", "zebibytes"],
                CommonTargets = ["EiB", "YiB", "ZB"],
                SuggestionPriority = 50,
                Tags = ["digital", "binary", "IEC", "massive", "bytes"],
                Description = "1024 exbibytes (binary)"
            },
            new UnitDescriptor
            {
                Id = "YiB",
                DisplayName = "Yobibyte",
                Symbol = "YiB",
                System = "Digital",
                IsCommon = false,
                Aliases = ["yobibyte", "yobibytes"],
                CommonTargets = ["ZiB", "YB"],
                SuggestionPriority = 55,
                Tags = ["digital", "binary", "IEC", "massive", "bytes"],
                Description = "1024 zebibytes (binary)"
            }
        ];
    }

    private static double ToBitsFactor(string unit)
    {
        return unit switch
        {
            "b" => 1,
            "nibble" => 4,
            "B" => 8,
            
            // Decimal bit units (SI prefixes)
            "kb" => 1000,
            "Mb" => 1000 * 1000,
            "Gb" => 1000.0 * 1000 * 1000,
            "Tb" => 1000.0 * 1000 * 1000 * 1000,
            "Pb" => 1000.0 * 1000 * 1000 * 1000 * 1000,
            "Eb" => 1000.0 * 1000 * 1000 * 1000 * 1000 * 1000,
            "Zb" => 1000.0 * 1000 * 1000 * 1000 * 1000 * 1000 * 1000,
            "Yb" => 1000.0 * 1000 * 1000 * 1000 * 1000 * 1000 * 1000 * 1000,
            
            // Binary bit units (IEC prefixes)
            "Kib" => 1024,
            "Mib" => 1024 * 1024,
            "Gib" => 1024.0 * 1024 * 1024,
            "Tib" => 1024.0 * 1024 * 1024 * 1024,
            "Pib" => 1024.0 * 1024 * 1024 * 1024 * 1024,
            "Eib" => 1024.0 * 1024 * 1024 * 1024 * 1024 * 1024,
            
            // Decimal byte units (SI prefixes)
            "kB" => 8 * 1000,
            "MB" => 8 * 1000 * 1000,
            "GB" => 8 * 1000.0 * 1000 * 1000,
            "TB" => 8 * 1000.0 * 1000 * 1000 * 1000,
            "PB" => 8 * 1000.0 * 1000 * 1000 * 1000 * 1000,
            "EB" => 8 * 1000.0 * 1000 * 1000 * 1000 * 1000 * 1000,
            "ZB" => 8 * 1000.0 * 1000 * 1000 * 1000 * 1000 * 1000 * 1000,
            "YB" => 8 * 1000.0 * 1000 * 1000 * 1000 * 1000 * 1000 * 1000 * 1000,
            
            // Binary byte units (IEC prefixes)
            "KiB" => 8 * 1024,
            "MiB" => 8 * 1024 * 1024,
            "GiB" => 8 * 1024.0 * 1024 * 1024,
            "TiB" => 8 * 1024.0 * 1024 * 1024 * 1024,
            "PiB" => 8 * 1024.0 * 1024 * 1024 * 1024 * 1024,
            "EiB" => 8 * 1024.0 * 1024 * 1024 * 1024 * 1024 * 1024,
            "ZiB" => 8 * 1024.0 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024,
            "YiB" => 8 * 1024.0 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024,
            
            _ => 0
        };
    }
}
