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
    private IListItem[]? _results;
    private readonly ListItem _showHelpItem;
    private readonly ISettingsManager _settings;

    public UnitConverterExtensionPage(ISettingsManager settingsManager)
        : this(settingsManager, null, string.Empty)
    {
    }

    private UnitConverterExtensionPage(ISettingsManager settingsManager, ConversionService? conversionService, string query)
    {
        ArgumentNullException.ThrowIfNull(settingsManager);

        this.Icon = Icons.AppLogoBitmap;
        this.Title = ConverterText.Get("Unit Converter");
        this.Name = ConverterText.Get("Convert units");
        this.Id = "com.jpsoftworks.cmdpal.unitconverter.index";

        _settings = settingsManager;
        this._conversionService = conversionService ?? new ConversionService(
            new SettingsUnitPreferenceProvider(_settings), new SettingsAccuracyPreferenceProvider(_settings), CultureInfo.CurrentUICulture);
        this._showHelpItem = new ListItem(new HelpPage(settingsManager))
        {
            Subtitle = ConverterText.Get("Show detailed help and examples"),
        };

        this.SetSearchNoUpdate(query);
        this._results = string.IsNullOrWhiteSpace(query) ? this.GetExampleConversions() : null;
    }

    internal UnitConverterExtensionPage CreateQueryPage(string query, string id) => new(_settings, _conversionService, query)
    {
        Id = id,
        Name = ConverterText.Get("Open in Unit Converter")
    };

    public override IListItem[] GetItems()
    {
        return this._results ??= this.GetConversionResults(this.SearchText);
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
        var culture = ResolveCulture(_settings.ParsingCulture);
        var wordLanguage = ResolveWordLanguage();
        this.PlaceholderText = ConverterText.Format("Enter conversion (e.g., '{0}', '{1}', '{2}')", CultureInfo.CurrentUICulture,
            this._conversionService.FormatConversionQuery("1 m", "ft", culture, wordLanguage),
            this._conversionService.FormatConversionQuery("100°C", "°F", culture, wordLanguage), "60 min");
        (string Section, (UnitType Type, string Query)[] Examples)[] groups =
        [
            ("Everyday conversions",
            [
                (UnitType.Length, "1 m to ft"), (UnitType.Temperature, "100°C to °F"),
                (UnitType.Time, "60 min"), (UnitType.Volume, "1 gal to l"),
                (UnitType.Mass, "1 kg to lb"), (UnitType.Area, "1 m² to ft²"),
                (UnitType.Volume, "1 m³ to l"), (UnitType.Volume, "1 cup to ml"),
                (UnitType.Speed, "100 km/h to mph"),
                (UnitType.Speed, "5:30 min/km to km/h"), (UnitType.Speed, "12 km/h to min/km"),
                (UnitType.FuelEconomy, $"{6.5.ToString(culture)} L/100km to mpg US")
            ]),
            ("Science and engineering",
            [
                (UnitType.TemperatureDelta, "10 Δ°C to Δ°F"),
                (UnitType.Frequency, "60 rpm to Hz"), (UnitType.Force, "1 lbf to N"),
                (UnitType.Torque, "100 N·m to lbf·ft"), (UnitType.Acceleration, "1 g0 to m/s²"),
                (UnitType.Density, "1 g/cm³ to kg/m³"), (UnitType.Voltage, $"{3.3.ToString(culture)} V to mV"),
                (UnitType.Current, "500 mA to A"), (UnitType.Resistance, $"{4.7.ToString(culture)} kohm to ohm"),
                (UnitType.Energy, "1 kWh to J"), (UnitType.Power, "100 W to hp"),
                (UnitType.Angle, "180 deg to rad"), (UnitType.Pressure, "1 atm to Pa")
            ]),
            ("Data and computing",
            [
                (UnitType.Data, "1 GB to MB"), (UnitType.DataRate, "100 Mbps to MB/s")
            ]),
            ("Mixed units and expressions",
            [
                (UnitType.Length, "1 ft 2 in to cm"), (UnitType.Length, "5'11\" to cm"),
                (UnitType.Length, "180 cm to ft in"), (UnitType.Time, "3661 s to h min s"),
                (UnitType.Length, "(2 + 3) / 2 m to cm"), (UnitType.Length, "2^3 m to cm")
            ])
        ];
        return [this._showHelpItem, .. groups.SelectMany(group => group.Examples
            .Select(example => (example.Type, Parts: example.Query.Split(" to ", 2, StringSplitOptions.None)))
            .Select(example => (example.Type, Query: example.Parts.Length == 2
                ? this._conversionService.FormatConversionQuery(example.Parts[0], example.Parts[1], culture, wordLanguage)
                : example.Parts[0]))
            .Select(example => new ListItem(CreateQueryCommand(example.Query))
        {
            Title = $"{ConverterText.Get(example.Type.ToString())}: {example.Query}",
            Subtitle = ConverterText.Get("Try this conversion"),
            Icon = GetIcon(new UnitToken { Type = example.Type }),
            TextToSuggest = example.Query
        }).Prepend(CreateSectionHeader(group.Section)))];
    }

    private static ListItem CreateSectionHeader(string title)
    {
        var section = ConverterText.Get(title).TrimEnd('.', '。');
        return new ListItem { Title = section, Section = section, Command = null };
    }

    private AnonymousCommand CreateQueryCommand(string query) => new(() =>
    {
        this.SearchText = query;
        // DynamicListPage suppresses the notification needed to update the host's search box.
        this.OnPropertyChanged(nameof(this.SearchText));
    })
    {
        Name = ConverterText.Get("Use this query"),
        Result = CommandResult.KeepOpen()
    };

    private IListItem[] GetConversionResults(string query)
    {
        var items = new List<IListItem>();

        try
        {
            var result = ConvertQuery(query);

            if (result.Success)
            {
                items.Add(CreateResultItem(result.Output, result.FormattedResult, result.OutputComponents));
                items.AddRange(GetInterpretationItems(result.OtherInterpretations));

                if (result.Suggestions.Length > 0)
                {
                    items.Add(CreateSectionHeader("Suggested conversions"));
                    items.AddRange(result.Suggestions.Take(5).Select(suggestion =>
                        CreateResultItem(suggestion.Output, suggestion.FormattedResult)));
                }

                var additional = this._conversionService.GetAdditionalConversions(result, ResolveCulture(_settings.ParsingCulture))
                    .Take(5).ToArray();
                if (additional.Length > 0)
                {
                    items.Add(CreateSectionHeader("More conversions"));
                    items.AddRange(additional.Select(suggestion => CreateResultItem(suggestion.Output, suggestion.FormattedResult)));
                }
            }
            else if (result.OtherInterpretations.Length > 0)
            {
                if (!result.IsIncomplete)
                {
                    items.Add(new ListItem(new NoOpCommand())
                    {
                        Title = ConverterText.Get("Conversion Error"),
                        Subtitle = result.ErrorMessage,
                        Icon = Icons.Warning
                    });
                }
                items.AddRange(GetInterpretationItems(result.OtherInterpretations));
                items.Add(this._showHelpItem);
            }
            else
            {
                if (result.IsIncomplete && result.Completions.Length > 0)
                {
                    items.Add(CreateSectionHeader(result.ErrorMessage));
                }
                else
                {
                    items.Add(new ListItem(new NoOpCommand())
                    {
                        Title = ConverterText.Get(result.IsIncomplete ? "Complete the conversion" : "Conversion Error"),
                        Subtitle = result.ErrorMessage,
                        Icon = result.IsIncomplete ? Icons.Suggestion : Icons.Warning
                    });
                }

                items.AddRange(result.Completions.Select(completion => new ListItem(CreateQueryCommand(completion.Query))
                {
                    Title = completion.FormattedResult.Length > 0 ? completion.FormattedResult : completion.Query,
                    Subtitle = completion.FormattedResult.Length > 0 ? GetUnitDisplayName(completion.Unit) : BuildSubtitle(completion.Unit),
                    Icon = GetIcon(completion.Unit),
                    TextToSuggest = completion.Query
                }));
                items.Add(this._showHelpItem);
            }
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Unexpected error while converting units");
            items.Add(new ListItem(new NoOpCommand())
            {
                Title = ConverterText.Get("Unexpected Error"),
                Subtitle = ConverterText.Get("An unexpected error occurred. Please try again."),
                Icon = Icons.Bug
            });
        }

        return items.ToArray();
    }

    private IListItem[] GetInterpretationItems(QueryCompletion[] interpretations)
    {
        return interpretations.Length == 0 ? [] :
        [
            CreateSectionHeader("Other interpretations"),
            .. interpretations.Select(interpretation => new ListItem(CreateQueryCommand(interpretation.Query))
            {
                Title = interpretation.Query,
                Subtitle = interpretation.FormattedResult,
                Icon = GetIcon(interpretation.Unit),
                TextToSuggest = interpretation.Query
            })
        ];
    }

    internal ListItem CreateResultItem(Quantity output, string formattedResult, Quantity[]? components = null)
    {
        var culture = ResolveCulture(_settings.ParsingCulture);
        var number = this._conversionService.FormatNumber(output.Value, culture);
        var suffix = output.Unit.Dimension switch { UnitDimension.Squared => "²", UnitDimension.Cubic => "³", _ => "" };
        var symbol = output.Unit.Metadata?.Symbol ?? output.Unit.Unit;
        var precise = $"{output.Value.ToString("R", culture)} {symbol}{suffix}";
        var numberLabel = ConverterText.Get("Copy number");
        if (output.Unit.Unit is "min/km" or "min/mi")
        {
            numberLabel = ConverterText.Format("Copy number in {0}", CultureInfo.CurrentUICulture, symbol);
        }

        if (components is { Length: > 0 })
        {
            precise = string.Join(" ", components.Select(part => $"{part.Value.ToString("R", culture)} {part.Unit.Metadata?.Symbol ?? part.Unit.Unit}"));
            numberLabel = ConverterText.Format("Copy number in {0}", CultureInfo.CurrentUICulture, symbol);
        }
        return new ListItem(new CopyTextCommand(formattedResult) { Name = ConverterText.Get("Copy result") })
        {
            Title = formattedResult,
            Subtitle = BuildSubtitle(output.Unit),
            Icon = GetIcon(output.Unit),
            MoreCommands =
            [
                new CommandContextItem(new CopyTextCommand(number) { Name = numberLabel }),
                new CommandContextItem(new CopyTextCommand(precise) { Name = ConverterText.Get("Copy full precision") })
            ]
        };
    }

    private static string BuildSubtitle(UnitToken token) =>
        $"{ConverterText.Get(token.Type.ToString())} • {GetUnitDisplayName(token)}";

    private static string GetUnitDisplayName(UnitToken token)
    {
        var name = token.Metadata?.DisplayName;
        if (string.IsNullOrWhiteSpace(name))
        {
            name = token.Unit;
        }

        // Append dimension indicator for non-linear inputs (squared/cubic)
        return token.Dimension switch
        {
            UnitDimension.Squared => name + "²",
            UnitDimension.Cubic => name + "³",
            _ => name
        };
    }

    internal ConversionResult ConvertQuery(string query) =>
        this._conversionService.ConvertQuery(query, ResolveCulture(_settings.ParsingCulture), ResolveWordLanguage());

    private ConversionWordLanguage ResolveWordLanguage() =>
        _settings.ParsingCulture == PreferredCulture.ForceEnglishCulture &&
        _settings.ConversionWordLanguage is (ConversionWordLanguage.Default or ConversionWordLanguage.InterfaceInputAndEnglish)
            ? ConversionWordLanguage.InputLanguageAndEnglish : _settings.ConversionWordLanguage;

    internal static CultureInfo ResolveCulture(PreferredCulture preferred)
    {
        return preferred switch
        {
            PreferredCulture.ForceEnglishCulture => CultureInfo.GetCultureInfo("en-US"),
            PreferredCulture.UseCurrentCulture => CultureInfo.CurrentCulture,
            _ => CultureInfo.CurrentCulture
        };
    }

    internal static IconInfo GetIcon(UnitToken outputUnit)
    {
        return outputUnit.Type switch
        {
            UnitType.Length => Icons.Units.Length,
            UnitType.Area => Icons.Units.Area,
            UnitType.Volume => Icons.Units.Volume,
            UnitType.Temperature => Icons.Units.Temperature,
            UnitType.TemperatureDelta => Icons.Units.TemperatureDelta,
            UnitType.Frequency => Icons.Units.Frequency,
            UnitType.Force => Icons.Units.Force,
            UnitType.Torque => Icons.Units.Torque,
            UnitType.Acceleration => Icons.Units.Acceleration,
            UnitType.Density => Icons.Units.Density,
            UnitType.Voltage => Icons.Units.Voltage,
            UnitType.Current => Icons.Units.Current,
            UnitType.Resistance => Icons.Units.Resistance,
            UnitType.Time => Icons.Units.Time,
            UnitType.Mass => Icons.Units.Mass,
            UnitType.Speed => Icons.Units.Speed,
            UnitType.Power => Icons.Units.Power,
            UnitType.Energy => Icons.Units.Energy,
            UnitType.Data => Icons.Units.Data,
            UnitType.DataRate => Icons.Units.DataRate,
            UnitType.FuelEconomy => Icons.Units.FuelEconomy,
            UnitType.Pressure => Icons.Units.Pressure,
            UnitType.Angle => Icons.Units.Angle,
            _ => Icons.Units.Generic
        };
    }

}