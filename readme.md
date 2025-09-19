<div align="center">

<p>
	<img src="art/StoreLogo.svg" alt="Logo" width="200" height="200">
</p>

<h1 align="center"><span style="font-weight: bold">Unit Converter</span> <br /><span style="font-weight: 300; opacity: 0.5">for Command Palette</span></h1>

</div>

This extension enables instant unit conversion directly within the [Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview). Effortlessly convert between different units of measurement—no need to leave your workflow.

## Features

- **Instant Conversion**: Convert between various units directly in Command Palette
- **Wide Unit Support**: Support for length, weight, temperature, volume, and more units using UnitsNet library
- **Smart Query Parsing**: Natural language input for conversion queries
- **Seamless Integration**: Built specifically for Microsoft PowerToys Command Palette

## Installation

> **Note:** This extension requires [Microsoft PowerToys](https://apps.microsoft.com/detail/xp89dcgq3k6vld) to be installed.

### Manual Installation

1. Download the latest release
2. Install the MSIX package
3. The extension will be available in Command Palette

## Usage

Open Command Palette and start typing unit conversion queries like:
- "10 meters to feet"
- "32 fahrenheit to celsius" 
- "5 pounds to kilograms"
- "1 gallon to liter"
- "30 squirrels to nuts"

## Supported Unit Categories

Category | Examples | Notes
:--|:--|:--
[Length](units.md) | m, cm, mm, km, in, ft, yd, mi, nmi | Supports squared and cubic via `^2`/`^3`
[Mass and Weight](units.md) | g, kg, lb, oz, stone, t | Common metric and imperial
[Temperature](units.md) | °C, °F, K, °R | Handles offsets correctly
[Time](units.md) | s, ms, min, h, d, w | Parsing robust to abbreviations
[Speed](units.md) | m/s, km/h, mph, ft/s, kn | Derived from length/time
[Volume](units.md) | l, ml, gal, qt, pt, cup, fl oz | Includes US customary
[Power](units.md) | W, kW, hp, ft·lb/min | Mechanical and electrical
[Energy](units.md) | J, kJ, cal, kcal, BTU, kWh, eV, ft·lb | Calorie ambiguity handled (see below)
[Pressure](units.md) | Pa, kPa, bar, atm, psi, mmHg | Common engineering units
[Data](units.md) | b, B, kb/kB, Mb/MB, KiB/MiB, … | Distinguishes bits vs bytes; SI vs IEC

## License

Apache License 2.0. See [LICENSE](LICENSE) for more information.

## Author

[Jiří Polášek](https://jiripolasek.com)