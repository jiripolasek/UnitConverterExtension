// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core;

/// <summary>
/// Indicates user preferences for resolving unit ambiguities.
/// Currently supports choosing US vs Imperial for common volume names.
/// </summary>
public interface IUnitPreferenceProvider
{
    VolumeSystemPreference VolumePreference { get; }
}

/// <summary>
/// Preference for interpreting unqualified volume unit names like 'gallon', 'quart', 'pint', 'fluid ounce'.
/// </summary>
public enum VolumeSystemPreference
{
    Auto = 0,
    US = 1,
    Imperial = 2
}

/// <summary>
/// Default implementation of unit preferences.
/// </summary>
public sealed class DefaultUnitPreferenceProvider : IUnitPreferenceProvider
{
    public DefaultUnitPreferenceProvider(VolumeSystemPreference volumePreference = VolumeSystemPreference.US)
    {
        VolumePreference = volumePreference;
    }

    public VolumeSystemPreference VolumePreference { get; }
}
