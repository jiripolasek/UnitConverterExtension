// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using System.Globalization;
using JPSoftworks.UnitConverter.Core;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Helpers;

internal sealed partial class SettingsManager : JsonSettingsManager, ISettingsManager
{
    private static string Namespaced(string propertyName) => $"jpsoftworks.unitconverter.{propertyName}";

    private static string DefaultLabel(string name) =>
        ConverterText.Format("Default ({0})", CultureInfo.CurrentUICulture, ConverterText.Get(name));

    private readonly ChoiceSetSetting _volumeSystemPreference = new(
        Namespaced(nameof(VolumeSystemPreference)),
        ConverterText.Get("Volume system preference"),
        ConverterText.Get("Choose the gallon, quart, pint, and fluid ounce used when US or Imperial is not specified."), [
            new ChoiceSetSetting.Choice(DefaultLabel("Auto"), VolumeSystemPreference.Auto.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("US customary"), VolumeSystemPreference.US.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Imperial (UK)"), VolumeSystemPreference.Imperial.ToString("G")),
        ]);

    public VolumeSystemPreference VolumeSystemPreference =>
        Enum.TryParse<UnitConverter.Core.VolumeSystemPreference>(this._volumeSystemPreference.Value, out var result)
            ? result
            : VolumeSystemPreference.Auto;


    private readonly ChoiceSetSetting _parsingCulture = new(
        Namespaced(nameof(ParsingCulture)),
        ConverterText.Get("Processing input"),
        ConverterText.Get("Sets number formatting and the input language. Default follows Windows regional settings."), [
            new ChoiceSetSetting.Choice(DefaultLabel("Use current culture"), PreferredCulture.Default.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Use English"), PreferredCulture.ForceEnglishCulture.ToString("G")),
        ]);

    public PreferredCulture ParsingCulture =>
        Enum.TryParse<PreferredCulture>(this._parsingCulture.Value, out var result)
            ? result
            : PreferredCulture.Default;

    private readonly ChoiceSetSetting _conversionWordLanguage = new(
        Namespaced(nameof(ConversionWordLanguage)),
        ConverterText.Get("Conversion words"),
        ConverterText.Get("Language of words such as to, in, or en"), [
            new ChoiceSetSetting.Choice(DefaultLabel("Automatic query languages"), ConversionWordLanguage.Default.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Automatic query languages"), ConversionWordLanguage.InterfaceInputAndEnglish.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Input language only"), ConversionWordLanguage.InputLanguageOnly.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Input language and English"), ConversionWordLanguage.InputLanguageAndEnglish.ToString("G")),
        ]);

    public ConversionWordLanguage ConversionWordLanguage =>
        Enum.TryParse<ConversionWordLanguage>(this._conversionWordLanguage.Value, out var result) && Enum.IsDefined(result)
            ? result
            : ConversionWordLanguage.Default;

    private readonly ChoiceSetSetting _accuracy = new(
        Namespaced(nameof(Accuracy)),
        ConverterText.Get("Accuracy"),
        ConverterText.Get("Display precision of results"), [
            new ChoiceSetSetting.Choice(DefaultLabel("Enhanced"), "Default"),
            new ChoiceSetSetting.Choice(ConverterText.Get("Common use"), AccuracyLevel.CommonUse.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Enhanced"), AccuracyLevel.Enhanced.ToString("G")),
            new ChoiceSetSetting.Choice(ConverterText.Get("Extremely precise"), AccuracyLevel.ExtremelyPrecise.ToString("G")),
        ]);

    public AccuracyLevel Accuracy => Enum.TryParse<AccuracyLevel>(this._accuracy.Value, out var a) ? a : AccuracyLevel.Enhanced;

    public SettingsManager()
        : this(BuildPath("settings.json"))
    {
    }

    internal SettingsManager(string filePath)
    {
        this.Settings.Add(this._volumeSystemPreference);
        this.Settings.Add(this._parsingCulture);
        this.Settings.Add(this._conversionWordLanguage);
        this.Settings.Add(this._accuracy);

        this.FilePath = filePath;
        this.LoadSettings();
        // The former explicit regional option has always behaved the same as Default.
        if (this._parsingCulture.Value == PreferredCulture.UseCurrentCulture.ToString("G"))
            this._parsingCulture.Value = PreferredCulture.Default.ToString("G");
        this.UpdateQueryLanguageLabel();

        this.Settings.SettingsChanged += this.OnSettingsOnSettingsChanged;
    }

    private void OnSettingsOnSettingsChanged(object o, Settings settings)
    {
        this.UpdateQueryLanguageLabel();
        this.SaveSettings();
    }

    private void UpdateQueryLanguageLabel()
    {
        var label = this.ParsingCulture == PreferredCulture.ForceEnglishCulture ? "English only" : "Automatic query languages";
        this._conversionWordLanguage.Choices[0].Title = DefaultLabel(label);
        this._conversionWordLanguage.Choices[1].Title = ConverterText.Get(label);
    }

    private static string BuildPath(string filename)
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return Path.Combine(directory, filename);
    }
}