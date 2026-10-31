# Changelog

All notable changes to Unit Converter for Command Palette are documented in this file.

<!--
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
-->

## [0.6.0] - 2026-11-01

Expanded unit coverage, more flexible queries, and localized guidance make everyday and engineering conversions easier to enter and copy.

### Highlights

- **23 conversion categories** - added acceleration, force, torque, density, frequency, fuel economy, voltage, electric current, electrical resistance, data rates, and temperature differences.
- **Mixed units and expressions** - combine measurements such as `1 ft 2 in`, or enter fractions, arithmetic, and scientific notation.
- **Combined output targets** - use `ft in`, `h min s`, or `lb oz` to split results into multiple units, such as `180 cm to ft in` for feet and inches.
- **14 languages** - localized unit names, conversion words, settings, help, and error messages, including Japanese, Korean, and Chinese queries without spaces.

### Added

- Running pace conversions, including clock notation such as `5:30 min/km to km/h`, and explicit US and Imperial fuel economy units.
- CSS and typography lengths, including reference pixels (`px`), quarter-millimeters (`Q`), DTP points and picas, and traditional printer points and picas. Use `dtp pt` or `css pt` for points; bare `pt` remains pint.
- Suggestions and result previews for incomplete queries, such as `1 m to`, with selectable completions and alternative interpretations.
- Context menu actions to copy just the number or the result at full precision, alongside the formatted result.
- A conversion-word setting to accept words from the input language alone or together with English. English unit names remain available alongside translated names.
- GitHub Actions workflows for CI, Microsoft Store package submission, draft GitHub releases, and WinGet updates.

### Changed

- Grouped examples, suggested conversions, and alternative interpretations under section headers.
- Expanded the length catalog, with additional unit spellings and an updated [supported units guide](units.md).
- Exact unit spellings take priority. Unambiguous casing variants are accepted, while ambiguous interpretations offer an explicit choice and preserve case-sensitive unit meanings.
- Updated category icons, including dedicated Mass and Angle icons.
- Moved MSIX packaging to a standalone Windows Application Packaging project for x64 and ARM64. Managed publishing is the default, with Native AOT available as an option.

### Fixed

- Selecting examples, completions, and alternative interpretations now fills the search field and refreshes the results.
- Conversion suggestions now include zero, negative, very small, and very large finite values.
- Corrected conflicting unit spellings and symbol resolution, including watts (`W`), microamperes (`uA`), and printer picas.

## [0.4.0] - 2025-09-25

### Added

- New unit categories, including Power, Energy, Pressure, Speed, Data, and Angle.
- Additional units in existing categories, including astronomical units.
- An option to set the preferred decimal precision.
- A setting to choose between US and Imperial volume units when the unit system is not explicitly specified.

### Changed

- Improved precision of conversion results.
- Smarter input parsing to recognize more formats.

## [0.2.0] - 2025-09-22

Initial public release.
