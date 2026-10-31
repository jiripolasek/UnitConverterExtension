namespace JPSoftworks.UnitConverter.Core;

// Share query vocabulary without changing regional numbers or unit preferences.
internal sealed class ConversionQueryCulture : CultureInfo
{
    internal CultureInfo[] Languages { get; }
    internal string CacheKey { get; }

    internal ConversionQueryCulture(CultureInfo numberCulture, CultureInfo uiCulture)
        : base(numberCulture.Name, numberCulture.UseUserOverride)
    {
        NumberFormat = numberCulture.NumberFormat;
        Languages = [uiCulture, numberCulture];
        CacheKey = $"{numberCulture.Name}|{uiCulture.Name}";
    }
}