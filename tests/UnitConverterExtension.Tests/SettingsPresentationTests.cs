using System.Globalization;
using System.Text.Json;
using JPSoftworks.UnitConverter.Core;
using JPSoftworks.UnitConverterExtension.Helpers;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace JPSoftworks.UnitConverterExtension.Tests;

public class SettingsPresentationTests
{
    [Theory]
    [InlineData("Default", "Default (Interface language, input language, and English)", "ForceEnglishCulture", "Default (English only)",
        ConversionWordLanguage.Default)]
    [InlineData("ForceEnglishCulture", "Default (English only)", "Default", "Default (Interface language, input language, and English)",
        ConversionWordLanguage.Default)]
    [InlineData("Default", "Default (Interface language, input language, and English)", "ForceEnglishCulture", "Default (English only)",
        ConversionWordLanguage.InterfaceInputAndEnglish)]
    [InlineData("ForceEnglishCulture", "Default (English only)", "Default", "Default (Interface language, input language, and English)",
        ConversionWordLanguage.InterfaceInputAndEnglish)]
    public void DefaultAndExplicitQueryLanguagesReflectTheLoadedAndSavedInputFormat(
        string initial, string title, string updated, string updatedTitle, ConversionWordLanguage mode)
    {
        var previousUi = CultureInfo.CurrentUICulture;
        var path = Path.GetTempFileName();
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            var input = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["jpsoftworks.unitconverter.ParsingCulture"] = initial,
                ["jpsoftworks.unitconverter.ConversionWordLanguage"] = mode.ToString()
            });
            File.WriteAllText(path, input);
            var settings = new SettingsManager(path);
            var languageField = GetField(settings, "ConversionWordLanguage");
            var choices = languageField.GetProperty("choices");
            Assert.Equal(4, choices.GetArrayLength());
            Assert.Equal("Default", choices[0].GetProperty("value").GetString());
            Assert.Equal("InterfaceInputAndEnglish", choices[1].GetProperty("value").GetString());
            Assert.Equal(mode.ToString(), languageField.GetProperty("value").GetString());
            Assert.Equal(title, choices[0].GetProperty("title").GetString());
            Assert.Equal(title["Default (".Length..^1], choices[1].GetProperty("title").GetString());
            var form = Assert.IsType<SettingsForm>(Assert.Single(settings.Settings.ToContent()));
            var changes = JsonSerializer.Serialize(new Dictionary<string, string> { ["jpsoftworks.unitconverter.ParsingCulture"] = updated });
            form.SubmitForm(changes, "{}");
            var updatedChoices = GetField(settings, "ConversionWordLanguage").GetProperty("choices");
            Assert.Equal(updatedTitle, updatedChoices[0].GetProperty("title").GetString());
            Assert.Equal(updatedTitle["Default (".Length..^1], updatedChoices[1].GetProperty("title").GetString());
            var reloaded = new SettingsManager(path);
            Assert.Equal(settings.ParsingCulture, reloaded.ParsingCulture);
            Assert.Equal(mode, reloaded.ConversionWordLanguage);
            Assert.Equal(updatedTitle, GetField(reloaded, "ConversionWordLanguage").GetProperty("choices")[0].GetProperty("title").GetString());
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousUi;
            File.Delete(path);
        }
    }

    [Fact]
    public void FormerRegionalChoiceLoadsAsDefaultWithoutChangingOtherPreferences()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """
                {
                    "jpsoftworks.unitconverter.ParsingCulture": "UseCurrentCulture",
                    "jpsoftworks.unitconverter.VolumeSystemPreference": "Imperial",
                    "jpsoftworks.unitconverter.ConversionWordLanguage": "InputLanguageOnly",
                    "jpsoftworks.unitconverter.Accuracy": "Enhanced"
                }
                """);
            var settings = new SettingsManager(path);
            Assert.Equal(PreferredCulture.Default, settings.ParsingCulture);
            Assert.Equal(VolumeSystemPreference.Imperial, settings.VolumeSystemPreference);
            Assert.Equal(ConversionWordLanguage.InputLanguageOnly, settings.ConversionWordLanguage);
            Assert.Equal(AccuracyLevel.Enhanced, settings.Accuracy);
            var field = GetField(settings, "ParsingCulture");
            Assert.Equal("Default", field.GetProperty("value").GetString());
            Assert.Collection(field.GetProperty("choices").EnumerateArray(),
                choice => Assert.Equal("Default", choice.GetProperty("value").GetString()),
                choice => Assert.Equal("ForceEnglishCulture", choice.GetProperty("value").GetString()));
            settings.SaveSettings();
            Assert.Equal(settings.ParsingCulture, new SettingsManager(path).ParsingCulture);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static JsonElement GetField(SettingsManager settings, string name)
    {
        var form = Assert.IsType<SettingsForm>(Assert.Single(settings.Settings.ToContent()));
        using var json = JsonDocument.Parse(form.TemplateJson);
        return json.RootElement.GetProperty("body").EnumerateArray()
            .Single(field => field.GetProperty("id").GetString() == "jpsoftworks.unitconverter." + name).Clone();
    }
}