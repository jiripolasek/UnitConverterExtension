<div align="center">

<p>
	<img src="art/StoreLogo.svg" alt="Logo" width="200" height="200">
</p>

<h1 align="center"><span style="font-weight: bold">Unit Converter</span> <br /><span style="font-weight: 300; opacity: 0.5">for Command Palette</span></h1>

</div>

Convert measurements without leaving your workflow. Type a query in [PowerToys Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview) and copy the result.

See the [changelog](CHANGELOG.md) for release notes.

## Features

- **Everyday and specialist conversions** across 23 categories, from length and temperature to electrical units and data rates.
- **Flexible input** with full unit names, abbreviations, mixed measurements, fractions, and arithmetic.
- **Combined results** in feet and inches, hours and minutes, or pounds and ounces.
- **Suggestions as you type** to help you choose compatible units and finish a query.
- **Copy options** for the displayed result, just the number, or full precision.
- **14 languages** for unit names, settings, and help.

## Installation

> **Note:** This extension requires [Microsoft PowerToys](https://apps.microsoft.com/detail/xp89dcgq3k6vld) to be installed.

### Microsoft Store installation (recommended)

<a href="https://apps.microsoft.com/detail/9n46rm40vr8d"><img alt="Get it from Microsoft" width="240px" src="https://get.microsoft.com/images/en-us%20dark.svg" /></a>

### Command Palette

[Open Unit Converter in Command Palette](x-cmdpal://extensions/gallery/jiripolasek.unit-converter), or visit the [extension gallery page](https://microsoft.github.io/CmdPal-Extensions/extensions/jiripolasek.unit-converter/) and choose **Open in Extension Gallery**.

### WinGet

Open a terminal and run:

```pwsh
winget install --id 9N46RM40VR8D --source msstore
```

### Manual installation

Download the MSIX installer from [Releases](https://github.com/jiripolasek/UnitConverterExtension/releases) and run it.

## Usage

Type a conversion directly in Command Palette, or open **Convert units** to browse examples, suggestions, and help.

| To convert | Try |
|---|---|
| Length | `10 meters to feet` |
| Temperature | `32 fahrenheit to celsius` |
| Mixed measurements | `1 ft 2 in to cm` |
| Height | `5'11" to cm` |
| Feet and inches | `180 cm to ft in` |
| Hours, minutes, and seconds | `3661 s to h min s` |
| An arithmetic expression | `(1 + 2) / 2 m to ft` |
| Running pace | `5:30 min/km to km/h` |
| Data transfer rate | `100 Mbps to MB/s` |
| Fuel economy | `6.5 L/100km to mpg US` |
| Temperature difference | `10 delta C to delta F` |

Select a result to copy it. Its context menu also offers **Copy number** and **Copy full precision**. For combined results, **Copy number** uses the first requested unit.

In root search, **Open in Unit Converter** opens a page for that query. This leaves the converter's main page search unchanged.

To choose a target unit from suggestions, start a query such as `1 m to`. Select an example or suggestion to fill it in.

A few useful tips:

- Unit symbols can be case-sensitive: `MB/s` means megabytes per second, while `Mb/s` means megabits per second.
- Use `mpg US` or `mpg UK` to choose the gallon definition for fuel economy.
- Use `delta C` and `delta F` for temperature differences, such as a rise or drop in temperature.

## Settings and languages

- **Result precision** controls how many digits appear in results. The default is **More digits**; **Fewer digits** and **Most digits** offer less or more precision.
- **Volume units** chooses US customary or Imperial definitions for names such as gallon and pint. The default follows the input region.
- **Input format** uses Windows regional settings by default, or can be set to **English (United States)**. This controls number formatting and the input language.
- **Query languages** offers **Default** and an explicit **Interface language, input language, and English** choice. They currently accept the same languages; the explicit choice is saved separately from Default. **Input language only** and **Input language and English** keep their vocabulary restrictions. Forcing English input keeps queries in English.

The interface, including unit display names and error messages, follows your Windows display language. Examples use `→` when a conversion word in that language is unavailable under your input settings. Supported languages are English, Czech, German, Spanish, French, Italian, Japanese, Korean, Dutch, Polish, Portuguese (Brazil and Portugal), and Chinese (Simplified and Traditional).

With a French interface and Czech regional settings, the default accepts `1,5 mètres en pieds`, `1,5 metry na stopy`, and `1.5 meters to feet`. Results keep French unit names and Czech number formatting. A translated unit name displayed in the interface is also accepted in a default query.

Regional number rules apply independently of query language. Valid regional grouping and western grouping such as `1,234.5` are accepted, including Swiss `1’000` and `1'000`. German input interprets `1.234` as 1234 and `1.5` as 1.5. A leading zero or scientific notation makes a single point or comma decimal: `0.500` and `0,500` mean 0.5, while `1.500e3` and `1,500e3` mean 1500. Grouped scientific notation and malformed grouping are rejected. Automatic US or Imperial volume definitions follow the selected input region, independently of interface language.

When a translated word has different unit meanings in the accepted languages, choose an explicit interpretation. Root search opens the converter's choices instead of copying a guess. Exact unit identifiers and case-sensitive symbols keep their existing meanings.

English unit names remain available alongside translated names. For example, with French input you can write `1 pied 2 pouces en centimètres`.

Japanese, Korean, and Chinese queries also work without spaces:

| Language | Example | Compact form |
|---|---|---|
| Japanese | `1メートルをフィートに変換` | `1mをftに` |
| Korean | `1미터를 피트로 변환` | `1m를ft로` |
| Simplified Chinese | `1米转换为英尺` | `1m转ft` |
| Traditional Chinese | `1公尺換算成英尺` | `1m轉ft` |

## Supported units

| Group | Categories |
|---|---|
| Everyday measurements | Length, area, angle, mass and weight, volume, time |
| Temperature | Temperature, temperature differences |
| Movement | Speed and pace, acceleration |
| Science and engineering | Force, torque, density, pressure, energy, power, frequency |
| Electrical | Voltage, electric current, electrical resistance |
| Data | Data size, data rate |
| Travel | Fuel economy |

See the [full list of supported units](units.md) for names, symbols, and abbreviations.

## Development

See the [development guide](docs/dev/README.md) for setup, builds, testing, and contributing.

## License

Apache License 2.0. See [LICENSE](LICENSE.txt) for more information.

## Author

[Jiří Polášek](https://jiripolasek.com)
