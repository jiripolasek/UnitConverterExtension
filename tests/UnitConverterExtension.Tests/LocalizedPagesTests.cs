using System.Globalization;
using System.Text.RegularExpressions;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using JPSoftworks.UnitConverterExtension.Pages;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class LocalizedPagesTests
{
    private static readonly string[] LocaleNames =
    [
        "en-US", "cs-CZ", "de-DE", "es-ES", "fr-FR", "it-IT", "ja-JP", "ko-KR",
        "nl-NL", "pl-PL", "pt-BR", "pt-PT", "zh-CN", "zh-TW"
    ];

    private static readonly (string Query, UnitType Type, string Unit, double Value)[] ExpectedExamples =
    [
        ("1 m to ft", UnitType.Length, "ft", 1 / 0.3048),
        ("100°C to °F", UnitType.Temperature, "°F", 212),
        ("60 min", UnitType.Time, "min", 60),
        ("1 gal to l", UnitType.Volume, "l", 3.785411784),
        ("1 kg to lb", UnitType.Mass, "lb", 1 / 0.45359237),
        ("1 m² to ft²", UnitType.Area, "ft", 1 / (0.3048 * 0.3048)),
        ("1 m³ to l", UnitType.Volume, "l", 1000),
        ("1 cup to ml", UnitType.Volume, "ml", 236.5882365),
        ("100 km/h to mph", UnitType.Speed, "mph", 100 / 1.609344),
        ("5:30 min/km to km/h", UnitType.Speed, "km/h", 60 / 5.5),
        ("12 km/h to min/km", UnitType.Speed, "min/km", 5),
        ("6.5 L/100km to mpg US", UnitType.FuelEconomy, "mpg US", 235.2145833333333 / 6.5),
        ("10 Δ°C to Δ°F", UnitType.TemperatureDelta, "Δ°F", 18),
        ("60 rpm to Hz", UnitType.Frequency, "Hz", 1),
        ("1 lbf to N", UnitType.Force, "N", 4.4482216152605),
        ("100 N·m to lbf·ft", UnitType.Torque, "lbf·ft", 100 / 1.3558179483314004),
        ("1 g0 to m/s²", UnitType.Acceleration, "m/s²", 9.80665),
        ("1 g/cm³ to kg/m³", UnitType.Density, "kg/m³", 1000),
        ("3.3 V to mV", UnitType.Voltage, "mV", 3300),
        ("500 mA to A", UnitType.Current, "A", 0.5),
        ("4.7 kohm to ohm", UnitType.Resistance, "ohm", 4700),
        ("1 kWh to J", UnitType.Energy, "J", 3600000),
        ("100 W to hp", UnitType.Power, "hp", 100 / 745.6998715822702),
        ("180 deg to rad", UnitType.Angle, "rad", Math.PI),
        ("1 atm to Pa", UnitType.Pressure, "Pa", 101325),
        ("1 GB to MB", UnitType.Data, "MB", 1000),
        ("100 Mbps to MB/s", UnitType.DataRate, "MB/s", 12.5),
        ("1 ft 2 in to cm", UnitType.Length, "cm", 35.56),
        ("5'11\" to cm", UnitType.Length, "cm", 180.34),
        ("180 cm to ft in", UnitType.Length, "ft", 1.8 / 0.3048),
        ("3661 s to h min s", UnitType.Time, "h", 3661.0 / 3600),
        ("(2 + 3) / 2 m to cm", UnitType.Length, "cm", 250),
        ("2^3 m to cm", UnitType.Length, "cm", 800)
    ];

    public static IEnumerable<object[]> Configurations =>
        from locale in LocaleNames
        from input in Enum.GetValues<PreferredCulture>()
        from words in Enum.GetValues<ConversionWordLanguage>()
        select new object[] { locale, input, words };

    [Theory]
    [MemberData(nameof(Configurations))]
    public void EveryDisplayedExampleAndInputHintProducesTheExpectedResult(
        string name, PreferredCulture input, ConversionWordLanguage words)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings
            {
                ParsingCulture = input,
                ConversionWordLanguage = words
            });
            Assert.Equal(ConverterText.Get("Unit Converter", culture), page.Title);
            var help = Assert.Single(page.GetItems(), row => row.Command is HelpPage);
            Assert.Equal(ConverterText.Get("Help", culture), help.Command.Name);
            var examples = page.GetItems().Where(row => row.Command is AnonymousCommand).ToArray();
            Assert.Equal(ExpectedExamples.Length, examples.Length);
            var firstQuery = input == PreferredCulture.ForceEnglishCulture ? name switch
            {
                "en-US" => "1 m to ft",
                "de-DE" or "it-IT" or "nl-NL" => "1 m in ft",
                _ => "1 m → ft"
            } : name switch
            {
                "ja-JP" => "1 mをftに",
                "ko-KR" => "1 m를 ft로",
                "zh-CN" => "1 m转ft",
                "zh-TW" => "1 m轉ft",
                _ => examples[0].TextToSuggest
            };
            Assert.Equal(firstQuery, examples[0].TextToSuggest);
            foreach (var (example, expected) in examples.Zip(ExpectedExamples))
            {
                var result = AssertConversion(page, example.TextToSuggest, expected.Type, expected.Unit, expected.Value);
                Assert.Equal(expected.Query == "1 m² to ft²" ? UnitDimension.Squared : UnitDimension.Linear, result.Output.Unit.Dimension);
                if (expected.Query == "180 cm to ft in")
                {
                    Assert.Equal(["ft", "in"], result.OutputComponents.Select(part => part.Unit.Unit));
                    Assert.Equal(5, result.OutputComponents[0].Value);
                    Assert.Equal(10.8661417322835, result.OutputComponents[1].Value, 8);
                }
                else if (expected.Query == "3661 s to h min s")
                {
                    Assert.Equal(["h", "min", "s"], result.OutputComponents.Select(part => part.Unit.Unit));
                    Assert.All(result.OutputComponents, part => Assert.Equal(1, part.Value, 8));
                }
                else
                {
                    Assert.Empty(result.OutputComponents);
                }

                Assert.Contains(example.TextToSuggest, example.Title);
                Assert.Equal(CommandResultKind.KeepOpen, Assert.IsType<AnonymousCommand>(example.Command).Invoke().Kind);
                Assert.Equal(example.TextToSuggest, page.SearchText);
                Assert.Equal(result.FormattedResult, Assert.IsType<CopyTextCommand>(page.GetItems()[0].Command).Text);
            }

            var hints = QuotedInputs(page.PlaceholderText);
            Assert.Equal(3, hints.Length);
            AssertConversion(page, hints[0], UnitType.Length, "ft", 1 / 0.3048);
            AssertConversion(page, hints[1], UnitType.Temperature, "°F", 212);
            AssertConversion(page, hints[2], UnitType.Time, "min", 60);

            var error = page.ConvertQuery("?");
            Assert.False(error.Success);
            var errorHints = QuotedInputs(error.ErrorMessage);
            Assert.Equal(2, errorHints.Length);
            AssertConversion(page, errorHints[0], UnitType.Length, "cm", 100);
            AssertConversion(page, errorHints[1], UnitType.Time, "min", 60);

            var parsingCulture = input == PreferredCulture.ForceEnglishCulture ? CultureInfo.GetCultureInfo("en-US") : culture;
            page.SearchText = $"1 m {ConverterText.GetConversionWords(parsingCulture, words)[0]}";
            var heading = page.GetItems()[0];
            Assert.Null(heading.Command);
            Assert.False(heading.Title.EndsWith('.') || heading.Title.EndsWith('。'));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    private static string[] QuotedInputs(string text) => Regex.Matches(text, "['«„“「](?<query>[^'«»„“”「」]+)['»“”」]")
        .Select(match => match.Groups["query"].Value.Trim()).ToArray();

    private static ConversionResult AssertConversion(UnitConverterExtensionPage page, string query, UnitType type, string unit, double value)
    {
        var result = page.ConvertQuery(query);
        Assert.True(result.Success, $"{CultureInfo.CurrentUICulture.Name}: {query}: {result.ErrorMessage}");
        Assert.Equal(type, result.Output.Unit.Type);
        Assert.Equal(unit, result.Output.Unit.Unit);
        Assert.Equal(value, result.Output.Value, 4);
        return result;
    }

    [Theory]
    [InlineData("cs-CZ", "en-US", PreferredCulture.Default)]
    [InlineData("cs-CZ", "en-US", PreferredCulture.UseCurrentCulture)]
    [InlineData("en-US", "cs-CZ", PreferredCulture.UseCurrentCulture)]
    [InlineData("cs-CZ", "cs-CZ", PreferredCulture.ForceEnglishCulture)]
    public void DisplayTextUsesTheUiLanguageWhenInputCultureDiffers(string inputName, string uiName, PreferredCulture preference)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(inputName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(uiName);
            var czech = uiName == "cs-CZ";
            var page = new UnitConverterExtensionPage(new FallbackConversionTests.TestSettings { ParsingCulture = preference });
            page.SearchText = "1";
            Assert.Equal(czech ? "Vyberte výchozí jednotku" : "Choose a source unit", page.GetItems()[0].Title);
            Assert.Equal(czech ? "Délka • metr" : "Length • Meter",
                Assert.Single(page.GetItems(), row => row.TextToSuggest == "1 m").Subtitle);

            page.SearchText = "1 m -> f";
            Assert.Equal(czech ? "Vyberte kompatibilní cílovou jednotku" : "Choose a compatible target unit", page.GetItems()[0].Title);
            Assert.Equal(czech ? "stopa" : "Foot", Assert.Single(page.GetItems(), row => row.TextToSuggest == "1 m -> ft").Subtitle);

            page.SearchText = "1 pt";
            Assert.Equal(czech ? "Objem • americká pinta" : "Volume • US Pint", page.GetItems()[0].Subtitle);
            var liters = Assert.Single(page.GetItems(), row => row.Title.EndsWith(" L", StringComparison.Ordinal));
            Assert.Equal(czech ? "Objem • litr" : "Volume • Liter", liters.Subtitle);
            Assert.Contains(preference == PreferredCulture.ForceEnglishCulture || inputName == "en-US" ? '.' : ',', liters.Title);
            Assert.Contains(page.GetItems(), row => row.TextToSuggest == "1 dtp pt");

            var compound = page.ConvertQuery("180 cm -> ft in");
            Assert.Equal(czech ? "stopa" : "Foot", compound.Output.Unit.Metadata!.DisplayName);
            Assert.Equal(czech ? "palec" : "Inch", compound.OutputComponents[1].Unit.Metadata!.DisplayName);
            AssertConversion(page, preference != PreferredCulture.ForceEnglishCulture && inputName == "cs-CZ"
                ? "1,5 metry na cm" : "1.5 meters to cm", UnitType.Length, "cm", 150);
            Assert.Equal(czech ? "Neznámá jednotka: unknownunit" : "Unknown unit: unknownunit", page.ConvertQuery("1 unknownunit").ErrorMessage);
            Assert.Equal(czech ? "Dokončete výpočet a poté zadejte jednotku." : "Complete the calculation, then enter a unit.",
                page.ConvertQuery("1 +").ErrorMessage);

            page.SearchText = "10 mn -> N";
            Assert.Equal(czech ? "Další interpretace" : "Other interpretations", page.GetItems()[0].Title);
            Assert.Equal(czech ? "Vyberte význam jednotky." : "Choose a unit interpretation.", page.ConvertQuery(page.SearchText).ErrorMessage);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Theory]
    [InlineData("cs-CZ", "1 m na ft")]
    [InlineData("ja-JP", "1 mをftに")]
    [InlineData("ko-KR", "1 m를 ft로")]
    [InlineData("zh-CN", "1 m转ft")]
    [InlineData("zh-TW", "1 m轉ft")]
    public void InputHintsAvoidForeignWordsAndRemainUsableWhenTheUiLanguageDiffers(string name, string nativeQuery)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var settings = new FallbackConversionTests.TestSettings
            {
                ParsingCulture = PreferredCulture.UseCurrentCulture,
                ConversionWordLanguage = ConversionWordLanguage.InputLanguageOnly
            };
            var page = new UnitConverterExtensionPage(settings);
            Assert.Equal("1 m → ft", QuotedInputs(page.PlaceholderText)[0]);
            AssertConversion(page, "1 m → ft", UnitType.Length, "ft", 1 / 0.3048);
            AssertConversion(page, nativeQuery, UnitType.Length, "ft", 1 / 0.3048);
            settings.ConversionWordLanguage = ConversionWordLanguage.InputLanguageAndEnglish;
            page.SearchText = " ";
            Assert.Equal("1 m to ft", QuotedInputs(page.PlaceholderText)[0]);
            settings.ParsingCulture = PreferredCulture.ForceEnglishCulture;
            page.SearchText = "1 m to ft";
            page.SearchText = "";
            Assert.Equal("1 m to ft", QuotedInputs(page.PlaceholderText)[0]);
            Assert.DoesNotContain(page.GetItems(), row => row.TextToSuggest == nativeQuery);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public void UnexpectedExceptionsShowLocalizedTextInsteadOfRawDetails()
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("cs-CZ");
            var page = new UnitConverterExtensionPage(new ThrowingAccuracySettings()) { SearchText = "1 m to cm" };
            var row = Assert.Single(page.GetItems());
            Assert.Equal("Neočekávaná chyba", row.Title);
            Assert.Equal("Došlo k neočekávané chybě. Zkuste to znovu.", row.Subtitle);
            Assert.DoesNotContain("raw exception details", row.Subtitle);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private sealed class ThrowingAccuracySettings : ISettingsManager
    {
        public VolumeSystemPreference VolumeSystemPreference => VolumeSystemPreference.US;
        public PreferredCulture ParsingCulture => PreferredCulture.ForceEnglishCulture;
        public ConversionWordLanguage ConversionWordLanguage => ConversionWordLanguage.Default;
        public AccuracyLevel Accuracy => throw new InvalidOperationException("raw exception details");
    }
}
