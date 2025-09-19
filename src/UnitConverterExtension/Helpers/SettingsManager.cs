// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverter.Core;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Helpers;

internal sealed partial class SettingsManager : JsonSettingsManager, ISettingsManager
{
    private static string Namespaced(string propertyName) => $"jpsoftworks.unitconverter.{propertyName}";

    private readonly ChoiceSetSetting _volumeSystemPreference = new(
        Namespaced(nameof(VolumeSystemPreference)),
        "Volume system preference",
        "Volume system preference", [
            new ChoiceSetSetting.Choice("Auto", VolumeSystemPreference.Auto.ToString("G")),
            new ChoiceSetSetting.Choice("US", VolumeSystemPreference.US.ToString("G")),
            new ChoiceSetSetting.Choice("Imperial", VolumeSystemPreference.Imperial.ToString("G")),
        ]);

    public VolumeSystemPreference VolumeSystemPreference =>
        Enum.TryParse<UnitConverter.Core.VolumeSystemPreference>(this._volumeSystemPreference.Value, out var result)
            ? result
            : VolumeSystemPreference.Auto;


    private readonly ChoiceSetSetting _parsingCulture = new(
        Namespaced(nameof(ParsingCulture)),
        "Processing input",
        "Processing input", [
            new ChoiceSetSetting.Choice("Default", PreferredCulture.Default.ToString("G")),
            new ChoiceSetSetting.Choice("Use current culture", PreferredCulture.UseCurrentCulture.ToString("G")),
            new ChoiceSetSetting.Choice("Use English", PreferredCulture.ForceEnglishCulture.ToString("G")),
        ]);

    public PreferredCulture ParsingCulture =>
        Enum.TryParse<PreferredCulture>(this._parsingCulture.Value, out var result)
            ? result
            : PreferredCulture.Default;

    private readonly ChoiceSetSetting _accuracy = new(
        Namespaced(nameof(Accuracy)),
        "Accuracy",
        "Display precision of results", [
            new ChoiceSetSetting.Choice("Default (Enhanced)", "Default"),
            new ChoiceSetSetting.Choice("Common use", AccuracyLevel.CommonUse.ToString("G")),
            new ChoiceSetSetting.Choice("Enhanced", AccuracyLevel.Enhanced.ToString("G")),
            new ChoiceSetSetting.Choice("Extremely precise", AccuracyLevel.ExtremelyPrecise.ToString("G")),
        ]);

    public AccuracyLevel Accuracy => Enum.TryParse<AccuracyLevel>(this._accuracy.Value, out var a) ? a : AccuracyLevel.Enhanced;

    public SettingsManager()
    {
        this.Settings.Add(this._volumeSystemPreference);
        this.Settings.Add(this._parsingCulture);
        this.Settings.Add(this._accuracy);

        this.FilePath = BuildPath("settings.json");
        this.LoadSettings();

        this.Settings.SettingsChanged += this.OnSettingsOnSettingsChanged;
    }

    private void OnSettingsOnSettingsChanged(object o, Settings settings)
    {
        this.SaveSettings();
    }

    private static string BuildPath(string filename)
    {
        var directory = Utilities.BaseSettingsPath("Microsoft.CmdPal");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return Path.Combine(directory, filename);
    }
}