// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.Helpers;

internal sealed class SettingsUnitPreferenceProvider : IUnitPreferenceProvider
{
    private readonly ISettingsManager _settings;

    public SettingsUnitPreferenceProvider(ISettingsManager settings)
    {
        _settings = settings;
    }

    public VolumeSystemPreference VolumePreference => _settings.VolumeSystemPreference;
}

internal sealed class SettingsAccuracyPreferenceProvider : IAccuracyPreferenceProvider
{
    private readonly ISettingsManager _settings;

    public SettingsAccuracyPreferenceProvider(ISettingsManager settings)
    {
        _settings = settings;
    }

    public AccuracyLevel Accuracy => _settings.Accuracy;
}

internal sealed class FallbackSettingsManager : ISettingsManager
{
    public VolumeSystemPreference VolumeSystemPreference => VolumeSystemPreference.Auto;

    public PreferredCulture ParsingCulture => PreferredCulture.Default;

    public AccuracyLevel Accuracy => AccuracyLevel.CommonUse;
}