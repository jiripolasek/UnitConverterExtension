// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core.Modules;

/// <summary>
///     Time/Duration conversion module powered by Units.NET using the UnitDescriptor system
/// </summary>
internal class UnitsNetTimeModule : DescriptorBasedConversionModule
{
    public UnitsNetTimeModule() : base(CreateUnitDescriptors())
    {
    }

    public override string Category => "Time";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;

    protected override UnitType GetUnitType() => UnitType.Time;

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

            var duration = ParseDuration(value, from);
            if (duration.HasValue)
            {
                var converted = ConvertToUnit(duration.Value, to);
                if (converted.HasValue)
                {
                    result = converted.Value;
                    return true;
                }
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
                Id = "ns",
                DisplayName = "Nanosecond",
                Symbol = "ns",
                System = "Metric",
                IsCommon = false,
                Aliases = ["nanosecond", "nanoseconds"],
                CommonTargets = ["μs", "ms"],
                SuggestionPriority = 90,
                Tags = ["SI", "precise", "computing"],
                Description = "One billionth of a second"
            },
            new UnitDescriptor
            {
                Id = "μs",
                DisplayName = "Microsecond",
                Symbol = "μs",
                System = "Metric",
                IsCommon = false,
                Aliases = ["microsecond", "microseconds", "us"],
                CommonTargets = ["ms", "ns"],
                SuggestionPriority = 85,
                Tags = ["SI", "precise", "computing"],
                Description = "One millionth of a second"
            },
            new UnitDescriptor
            {
                Id = "ms",
                DisplayName = "Millisecond",
                Symbol = "ms",
                System = "Metric",
                IsCommon = true,
                Aliases = ["millisecond", "milliseconds"],
                CommonTargets = ["s", "μs"],
                SuggestionPriority = 30,
                Tags = ["SI", "computing", "timing"],
                Description = "One thousandth of a second"
            },
            new UnitDescriptor
            {
                Id = "s",
                DisplayName = "Second",
                Symbol = "s",
                System = "Metric",
                IsCommon = true,
                Aliases = ["sec", "second", "seconds"],
                CommonTargets = ["min", "ms", "h"],
                SuggestionPriority = 5,
                Tags = ["SI", "base", "common"],
                Description = "SI base unit of time"
            },
            new UnitDescriptor
            {
                Id = "min",
                DisplayName = "Minute",
                Symbol = "min",
                System = "Universal",
                IsCommon = true,
                Aliases = ["mins", "minute", "minutes"],
                CommonTargets = ["s", "h"],
                SuggestionPriority = 8,
                Tags = ["common", "daily"],
                Description = "60 seconds"
            },
            new UnitDescriptor
            {
                Id = "h",
                DisplayName = "Hour",
                Symbol = "h",
                System = "Universal",
                IsCommon = true,
                Aliases = ["hr", "hrs", "hour", "hours"],
                CommonTargets = ["min", "d"],
                SuggestionPriority = 10,
                Tags = ["common", "daily"],
                Description = "60 minutes"
            },
            new UnitDescriptor
            {
                Id = "d",
                DisplayName = "Day",
                Symbol = "d",
                System = "Universal",
                IsCommon = true,
                Aliases = ["day", "days"],
                CommonTargets = ["h", "w"],
                SuggestionPriority = 15,
                Tags = ["common", "daily", "calendar"],
                Description = "24 hours"
            },
            new UnitDescriptor
            {
                Id = "w",
                DisplayName = "Week",
                Symbol = "w",
                System = "Universal",
                IsCommon = true,
                Aliases = ["wk", "week", "weeks"],
                CommonTargets = ["d", "mo"],
                SuggestionPriority = 20,
                Tags = ["common", "calendar"],
                Description = "7 days"
            },
            new UnitDescriptor
            {
                Id = "mo",
                DisplayName = "Month",
                Symbol = "mo",
                System = "Universal",
                IsCommon = true,
                Aliases = ["month", "months"],
                CommonTargets = ["w", "d", "y"],
                SuggestionPriority = 25,
                Tags = ["calendar", "approximate"],
                Description = "Approximately 30.44 days"
            },
            new UnitDescriptor
            {
                Id = "y",
                DisplayName = "Year",
                Symbol = "y",
                System = "Universal",
                IsCommon = true,
                Aliases = ["yr", "year", "years"],
                CommonTargets = ["mo", "d"],
                SuggestionPriority = 30,
                Tags = ["calendar"],
                Description = "365.25 days (Julian year)"
            }
        ];
    }

    private static Duration? ParseDuration(double value, string unit)
    {
        return unit switch
        {
            "ns" => Duration.FromNanoseconds(value),
            "μs" => Duration.FromMicroseconds(value),
            "ms" => Duration.FromMilliseconds(value),
            "s" => Duration.FromSeconds(value),
            "min" => Duration.FromMinutes(value),
            "h" => Duration.FromHours(value),
            "d" => Duration.FromDays(value),
            "w" => Duration.FromWeeks(value),
            "mo" => Duration.FromDays(value * 30.44), // Approximate month
            "y" => Duration.FromJulianYears(value),
            _ => null
        };
    }

    private static double? ConvertToUnit(Duration duration, string unit)
    {
        return unit switch
        {
            "ns" => duration.Nanoseconds,
            "μs" => duration.Microseconds,
            "ms" => duration.Milliseconds,
            "s" => duration.Seconds,
            "min" => duration.Minutes,
            "h" => duration.Hours,
            "d" => duration.Days,
            "w" => duration.Weeks,
            "mo" => duration.Days / 30.44, // Approximate month
            "y" => duration.JulianYears,
            _ => null
        };
    }
}