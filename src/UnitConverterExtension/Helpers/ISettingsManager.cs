// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using JPSoftworks.UnitConverter.Core;

namespace JPSoftworks.UnitConverterExtension.Helpers;

public interface ISettingsManager
{
    VolumeSystemPreference VolumeSystemPreference { get; }

    PreferredCulture ParsingCulture { get; }

    ConversionWordLanguage ConversionWordLanguage { get; }

    AccuracyLevel Accuracy { get; }
}

public enum PreferredCulture
{
    Default,
    UseCurrentCulture,
    ForceEnglishCulture,
}