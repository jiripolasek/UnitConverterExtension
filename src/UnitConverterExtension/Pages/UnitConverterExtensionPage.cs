// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using JPSoftworks.UnitConverterExtension.Helpers;

namespace JPSoftworks.UnitConverterExtension.Pages;

internal sealed partial class UnitConverterExtensionPage : DynamicListPage
{
    private readonly ConversionService _conversionService;
    private IListItem[] _results;
    private readonly ListItem _showHelpItem;
    private readonly ISettingsManager _settings;

    public UnitConverterExtensionPage(ISettingsManager settingsManager)
    {
        ArgumentNullException.ThrowIfNull(settingsManager);

        this.Icon = Icons.AppLogoBitmap;
        this.Title = "Unit Converter";
        this.Name = "Convert Units";
        this.PlaceholderText = "Enter conversion (e.g., '1 m to ft', '100°C to °F', '60 mins')";
        this.Id = "com.jpsoftworks.cmdpal.unitconverter.index";

        _settings = settingsManager;
        var unitPref = new SettingsUnitPreferenceProvider(_settings);
        var accPref = new SettingsAccuracyPreferenceProvider(_settings);
        this._conversionService = new ConversionService(unitPref, accPref);
        this._showHelpItem = new ListItem(new HelpPage())
        {
            Subtitle = "Show detailed help and examples",
        };

        this._results = this.GetExampleConversions();
    }

    public override IListItem[] GetItems()
    {
        return this._results;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        if (string.IsNullOrWhiteSpace(newSearch))
        {
            this._results = this.GetExampleConversions();
        }
        else
        {
            this._results = this.GetConversionResults(newSearch);
        }

        this.RaiseItemsChanged();
    }

    private IListItem[] GetExampleConversions()
    {
        return
        [
            this._showHelpItem,
            new ListItem(new NoOpCommand())
            {
                Title = "Length: 1 m to ft → 3.281 ft",
                Subtitle = "Metric ↔ Imperial: mm, cm, m, km, in, ft, yd, mi",
                Icon = Icons.Units.Length
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Temperature: 100°C to °F → 212.0 °F",
                Subtitle = "Celsius, Fahrenheit, Kelvin, Rankine",
                Icon = Icons.Units.Temperature
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Time: 60 mins → 1.000 h, 3600.000 s",
                Subtitle = "Get multiple conversion suggestions",
                Icon = Icons.Units.Time
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Volume: 1 gal to l → 3.785 l (US) / 4.546 l (UK)",
                Subtitle = "Cultural detection: US vs Imperial gallons",
                Icon = Icons.Units.Volume
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Mass: 1 kg to lb → 2.205 lb",
                Subtitle = "Metric ↔ Imperial: mg, g, kg, oz, lb, stone",
                Icon = Icons.Units.Mass
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Area: 1 sq m to sq ft → 10.764 sq ft²",
                Subtitle = "Squared units: sq, ², ^2 formats supported",
                Icon = Icons.Units.Area
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Volume: 1 cu m to cu ft → 35.315 cu ft³",
                Subtitle = "Cubic units: cu, ³, ^3 formats supported",
                Icon = Icons.Units.Volume
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Cooking: 1 cup to ml → 236.588 ml",
                Subtitle = "tsp, tbsp, cup, fl oz for cooking",
                Icon = Icons.Units.Volume
            },
            new ListItem(new NoOpCommand())
            {
                // energy
                Title = "Energy: '1 kWh to J', '500 cal to kJ'",
                Subtitle = "Joule, calorie, BTU, kWh",
                Icon = Icons.Units.Energy
            },
            new ListItem(new NoOpCommand())
            {
                // power
                Title = "Power: '100 W to hp', '1 kW to BTU/h'",
                Subtitle = "Watt, horsepower, BTU per hour",
                Icon = Icons.Units.Power,
            },
            new ListItem(new NoOpCommand())
            {
                // speed
                Title = "Speed: '100 km/h to mph', '30 m/s to ft/s'",
                Subtitle = "km/h, m/s, mph, ft/s, knots",
                Icon = Icons.Units.Speed,
            },
            new ListItem(new NoOpCommand())
            {
                // data
                Title = "Data: '1 GB to MB', '5000 kb to MiB'",
                Subtitle = "bit, byte, kB, MB, GB, TB (decimal & binary)",
                Icon = Icons.Units.Data,
            },
            new ListItem(new NoOpCommand())
            {
                // angle
                Title = "Angle: '180 deg to rad', '3.14 rad to deg'",
                Subtitle = "Degrees, radians, gradians",
                Icon = Icons.Units.Generic,
            },
            new ListItem(new NoOpCommand())
            {
                // pressure
                Title = "Pressure: '1 atm to Pa', '30 psi to bar'",
                Subtitle = "Pascal, bar, psi, atm",
                Icon = Icons.Units.Pressure
            },
        ];
    }

    private IListItem[] GetConversionResults(string query)
    {
        var items = new List<IListItem>();

        try
        {
            var culture = ResolveCulture(_settings.ParsingCulture);
            var result = this._conversionService.ConvertQuery(query, culture);

            if (result.Success)
            {
                var copyCommand = new CopyTextCommand(result.FormattedResult);
                items.Add(new ListItem(copyCommand)
                {
                    Title = result.FormattedResult,
                    Icon = GetIcon(result.Output.Unit),
                    Subtitle = BuildSubtitle(result.Output.Unit)
                });

                if (result.Suggestions.Length > 0)
                {
                    items.Add(new ListItem(new NoOpCommand()) { Title = "Suggested conversions" });

                    foreach (var suggestion in result.Suggestions.Take(5))
                    {
                        var suggestionCopyCommand = new CopyTextCommand(suggestion.FormattedResult);
                        items.Add(new ListItem(suggestionCopyCommand)
                        {
                            Icon = GetIcon(suggestion.Output.Unit),
                            Title = suggestion.FormattedResult,
                            Subtitle = BuildSubtitle(suggestion.Output.Unit)
                        });
                    }
                }
            }
            else
            {
                items.Add(new ListItem(new NoOpCommand())
                {
                    Title = "Conversion Error",
                    Subtitle = result.ErrorMessage,
                    Icon = Icons.Warning
                });

                items.AddRange(GetHelpfulExamples());
            }
        }
        catch (Exception ex)
        {
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = "Unexpected Error",
                Subtitle = ex.Message,
                Icon = Icons.Bug
            });
        }

        return items.ToArray();
    }

    private static string BuildSubtitle(UnitToken token)
    {
        var name = token.Metadata?.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = token.Unit;
        }

        // Append dimension indicator for non-linear inputs (squared/cubic)
        var nameWithDim = token.Dimension switch
        {
            UnitDimension.Squared => name + "²",
            UnitDimension.Cubic => name + "³",
            _ => name
        };

        return $"{token.Type} • {nameWithDim}";
    }

    private static CultureInfo ResolveCulture(PreferredCulture preferred)
    {
        return preferred switch
        {
            PreferredCulture.ForceEnglishCulture => CultureInfo.GetCultureInfo("en-US"),
            PreferredCulture.UseCurrentCulture => CultureInfo.CurrentCulture,
            _ => CultureInfo.CurrentCulture
        };
    }

    private static IIconInfo? GetIcon(UnitToken outputUnit)
    {
        return outputUnit.Type switch
        {
            UnitType.Length => Icons.Units.Length,
            UnitType.Area => Icons.Units.Area,
            UnitType.Volume => Icons.Units.Volume,
            UnitType.Temperature => Icons.Units.Temperature,
            UnitType.Time => Icons.Units.Time,
            UnitType.Mass => Icons.Units.Mass,
            UnitType.Speed => Icons.Units.Speed,
            UnitType.Power => Icons.Units.Power,
            UnitType.Energy => Icons.Units.Energy,
            UnitType.Data => Icons.Units.Data,
            UnitType.Pressure => Icons.Units.Pressure,
            UnitType.Angle => Icons.Units.Generic,
            _ => Icons.Units.Generic
        };
    }

    private static IListItem[] GetHelpfulExamples()
    {
        return
        [
            new ListItem(new NoOpCommand())
            {
                Title = "Examples & Tips:", Subtitle = "Try these format patterns", Icon = Icons.Suggestion
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Length: '1 m to ft', '100 cm', '5 miles to km'",
                Subtitle = "Supports metric, imperial, nautical units",
                Icon = Icons.Units.Length
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Temperature: '100°C to °F', '32 F', '273 K'",
                Subtitle = "Celsius, Fahrenheit, Kelvin, Rankine",
                Icon = Icons.Units.Temperature
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Time: '60 mins', '2 hours to seconds', '1 day'",
                Subtitle = "From nanoseconds to years",
                Icon = Icons.Units.Time
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Volume: '1 gal to l', '500 ml', '1 cup to fl oz'",
                Subtitle = "US/Imperial units auto-detected by culture",
                Icon = Icons.Units.Volume
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Mass: '1 kg to lb', '16 oz to g', '1 stone'",
                Subtitle = "Metric and imperial weight units",
                Icon = Icons.Units.Mass
            },
            new ListItem(new NoOpCommand())
            {
                // energy
                Title = "Energy: '1 kWh to J', '500 cal to kJ'",
                Subtitle = "Joule, calorie, BTU, kWh",
                Icon = Icons.Units.Energy
            },
            new ListItem(new NoOpCommand())
            {
                // power
                Title = "Power: '100 W to hp', '1 kW to BTU/h'",
                Subtitle = "Watt, horsepower, BTU per hour",
                Icon = Icons.Units.Power,
            },
            new ListItem(new NoOpCommand())
            {
                // speed
                Title = "Speed: '100 km/h to mph', '30 m/s to ft/s'",
                Subtitle = "km/h, m/s, mph, ft/s, knots",
                Icon = Icons.Units.Speed,
            },
            new ListItem(new NoOpCommand())
            {
                // data
                Title = "Data: '1 GB to MB', '5000 kb to MiB'",
                Subtitle = "bit, byte, kB, MB, GB, TB (decimal & binary)",
                Icon = Icons.Units.Data,
            },
            new ListItem(new NoOpCommand())
            {
                // angle
                Title = "Angle: '180 deg to rad', '3.14 rad to deg'",
                Subtitle = "Degrees, radians, gradians",
                Icon = Icons.Units.Generic,
            },
            new ListItem(new NoOpCommand())
            {
                // pressure
                Title = "Pressure: '1 atm to Pa', '30 psi to bar'",
                Subtitle = "Pascal, bar, psi, atm",
                Icon = Icons.Units.Pressure
            },
            new ListItem(new NoOpCommand())
            {
                Title = "Formats: '1 m to ft', '1m->ft', '1 m as ft'",
                Subtitle = "Flexible input: to, as, ->, = indicators",
                Icon = Icons.Suggestion
            }
        ];
    }
}