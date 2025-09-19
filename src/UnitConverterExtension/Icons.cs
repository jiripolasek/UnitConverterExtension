// ------------------------------------------------------------
// 
// Copyright (c) Jiří Polášek. All rights reserved.
// 
// ------------------------------------------------------------

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension;

internal static class Icons
{
    public static IconInfo AppLogoVector { get; } = IconHelpers.FromRelativePath("Assets\\AppLogo.svg");

    public static IconInfo AppLogoBitmap { get; } = IconHelpers.FromRelativePath("Assets\\AppLogo.png");

    public static IconInfo Suggestion { get; } = new("\uEA80");

    public static IconInfo Warning { get; } = new("\uE7BA");

    public static IconInfo Bug { get; } = new("\uEBE8");

    public static IconInfo Help { get; } = new("\uE897");

    private static IconInfo Duo(string iconName) =>
        IconHelpers.FromRelativePaths($"Assets\\Icons\\{iconName}.light.svg", $"Assets\\Icons\\{iconName}.dark.svg");

    internal static class Units
    {
        public static IconInfo Length { get; } = Duo("FluentRuler20Regular");

        public static IconInfo Area { get; } = Duo("FluentTable20Regular");

        public static IconInfo Volume { get; } = Duo("FluentCube20Regular");

        public static IconInfo Speed { get; } = Duo("FluentPersonRunning20Regular");

        public static IconInfo Time { get; } = Duo("FluentClock20Regular");

        public static IconInfo Power { get; } = Duo("FluentFlash20Regular");

        public static IconInfo Temperature { get; } = Duo("FluentTemperature20Regular");

        public static IconInfo Mass { get; } = Duo("FluentScales20Regular");

        public static IconInfo Energy { get; } = Duo("FluentFire20Regular");

        public static IconInfo Data { get; } = Duo("FluentStorage20Regular");

        public static IconInfo Pressure { get; } = Duo("FluentGauge20Regular");

        public static IconInfo Currency { get; } = Duo("FluentMoney20Regular");

        public static IconInfo Generic { get; } = new("=");
    }
}