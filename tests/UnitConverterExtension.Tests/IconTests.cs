using System.Globalization;
using System.Xml.Linq;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Pages;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class IconTests
{
    [Theory]
    [InlineData(UnitType.Force, "FluentArrowSwap20Regular")]
    [InlineData(UnitType.Torque, "FluentArrowSync20Regular")]
    [InlineData(UnitType.Frequency, "FluentDeviceEq20Regular")]
    [InlineData(UnitType.Mass, "CalculatorWeight20Regular")]
    [InlineData(UnitType.Angle, "CalculatorAngle20Regular")]
    public void CategoriesUseTheirDedicatedIcons(UnitType type, string name)
    {
        var icon = UnitConverterExtensionPage.GetIcon(new UnitToken { Type = type });
        Assert.Equal(name + ".light.svg", Path.GetFileName(icon.Light.Icon));
        Assert.Equal(name + ".dark.svg", Path.GetFileName(icon.Dark.Icon));
    }

    [Fact]
    public void EveryCategoryHasDistinctReadableSvgColorsInBothThemes()
    {
        var lightColors = new HashSet<string>();
        var darkColors = new HashSet<string>();

        foreach (var type in Enum.GetValues<UnitType>().Where(type => type != UnitType.Unknown))
        {
            var icon = UnitConverterExtensionPage.GetIcon(new UnitToken { Type = type });
            Assert.NotSame(Icons.Units.Generic, icon);
            var light = XDocument.Load(icon.Light.Icon!);
            var dark = XDocument.Load(icon.Dark.Icon!);
            var lightColor = GetColor(light);
            var darkColor = GetColor(dark);

            Assert.True(lightColors.Add(lightColor), $"Duplicate light color for {type}.");
            Assert.True(darkColors.Add(darkColor), $"Duplicate dark color for {type}.");
            Assert.Equal("0 0 20 20", light.Root!.Attribute("viewBox")!.Value);
            Assert.Equal(light.ToString().Replace(lightColor, "#000000", StringComparison.Ordinal),
                dark.ToString().Replace(darkColor, "#000000", StringComparison.Ordinal));
            Assert.True(Contrast(lightColor, "#F0F0F0") >= 3, $"Insufficient light contrast for {type}.");
            Assert.True(Contrast(darkColor, "#333333") >= 3, $"Insufficient dark contrast for {type}.");
        }
    }

    private static string GetColor(XDocument svg)
    {
        var color = Assert.Single(svg.Descendants().Attributes("fill")
            .Select(attribute => attribute.Value).Where(value => value != "none").Distinct());
        Assert.Matches("^#[0-9A-F]{6}$", color);
        return color;
    }

    private static double Contrast(string foreground, string background)
    {
        var foregroundLuminance = Luminance(foreground);
        var backgroundLuminance = Luminance(background);
        return (Math.Max(foregroundLuminance, backgroundLuminance) + 0.05) /
            (Math.Min(foregroundLuminance, backgroundLuminance) + 0.05);
    }

    private static double Luminance(string color)
    {
        static double Channel(string value, int offset)
        {
            var channel = int.Parse(value.AsSpan(offset, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return channel <= 0.04045 ? channel / 12.92 : Math.Pow((channel + 0.055) / 1.055, 2.4);
        }

        return (Channel(color, 1) * 0.2126) + (Channel(color, 3) * 0.7152) + (Channel(color, 5) * 0.0722);
    }
}