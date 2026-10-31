// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

namespace JPSoftworks.UnitConverter.Core;

public enum AccuracyLevel
{
    CommonUse = 0,
    Enhanced = 1,
    ExtremelyPrecise = 2
}

public interface IAccuracyPreferenceProvider
{
    AccuracyLevel Accuracy { get; }
}

public sealed class DefaultAccuracyPreferenceProvider : IAccuracyPreferenceProvider
{
    public DefaultAccuracyPreferenceProvider(AccuracyLevel level = AccuracyLevel.CommonUse)
    {
        Accuracy = level;
    }

    public AccuracyLevel Accuracy { get; }
}
