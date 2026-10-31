# Unit category icon reference

[Development guide](README.md)

## Runtime mapping

[`Icons.Units`](../../src/UnitConverterExtension/Icons.cs) defines the category
assets. `UnitConverterExtensionPage.GetIcon` selects the mapping for examples,
results, completions, interpretations, and root fallback results. Unknown
categories use the `=` text icon; suggestion, warning, error, and help icons
use theme-aware font glyphs.

`Icons.Duo` constructs an `IconInfo` through `IconHelpers.FromRelativePaths`:

```text
Assets/Icons/<name>.light.svg
Assets/Icons/<name>.dark.svg
```

The [application project](../../src/UnitConverterExtension/JPSoftworks.UnitConverterExtension.csproj)
copies `Assets/Icons/**` to build and publish output with `PreserveNewest`.
The SVGs use a `0 0 20 20` view box. Light and dark variants share geometry
and differ only in fill color.

## Category assets

Colors use the [Fluent UI palette](https://github.com/microsoft/fluentui/blob/master/packages/tokens/src/global/colors.ts).
The light variants use `shade10`, except Power and Current, which use
`shade20`. Dark variants use `tint30`, except Pressure, which uses `tint40`.

| Category | SVG base name | Fluent ramp | Light | Dark |
|:--|:--|:--|:--|:--|
| Length | `FluentRuler20Regular` | blue | `#006CBF` | `#5CAAE5` |
| Area | `FluentTable20Regular` | teal | `#037679` | `#4CB4B7` |
| Volume | `FluentCube20Regular` | cyan | `#008AA9` | `#56BFD7` |
| Speed | `FluentPersonRunning20Regular` | green | `#0E700E` | `#54B054` |
| Time | `FluentClock20Regular` | cornflower | `#4760D5` | `#93A4F4` |
| Power | `FluentFlash20Regular` | marigold | `#B27C00` | `#F2C661` |
| Temperature | `FluentTemperature20Regular` | red | `#BC2F32` | `#E37D80` |
| Mass | `CalculatorWeight20Regular` | brown | `#804D29` | `#BB8F6F` |
| Energy | `FluentFire20Regular` | darkOrange | `#C43501` | `#E9835E` |
| Data | `FluentStorage20Regular` | purple | `#532982` | `#9470BD` |
| Pressure | `FluentGauge20Regular` | plum | `#6B0045` | `#D696C0` |
| Angle | `CalculatorAngle20Regular` | lavender | `#6656D1` | `#A79CF1` |
| Data Rate | `FluentArrowBidirectionalUpDown20Regular` | grape | `#7A1589` | `#B55FC1` |
| Fuel Economy | `FluentGasPump20Regular` | forest | `#427505` | `#85B44C` |
| Temperature Delta | `FluentTemperature20Regular.Delta` | cranberry | `#B10E1C` | `#DC626D` |
| Frequency | `FluentDeviceEq20Regular` | berry | `#AF33A1` | `#DA7ED0` |
| Force | `FluentArrowSwap20Regular` | pumpkin | `#B6480E` | `#DF8E64` |
| Torque | `FluentArrowSync20Regular` | lilac | `#9F3FAF` | `#CF87DA` |
| Acceleration | `FluentTopSpeed20Regular` | lightGreen | `#11910D` | `#5EC75A` |
| Density | `FluentScales20Regular.Density` | brass | `#89640A` | `#C1A256` |
| Voltage | `FluentFlash20Regular.Voltage` | orange | `#DE590B` | `#FAA06B` |
| Current | `FluentFlash20Regular.Current` | lightTeal | `#008B94` | `#58D3DB` |
| Resistance | `FluentFlash20Regular.Resistance` | steel | `#005265` | `#4496A9` |

## Geometry sources

- Category geometry comes from [Fluent UI System Icons](https://github.com/microsoft/fluentui-system-icons/tree/main/assets), except Mass and Angle.
- Mass and Angle use outlines from the [Calculator Fluent Icons font](https://github.com/microsoft/calculator/blob/main/src/Calculator/Assets/CalculatorIcons.ttf): weight is U+F4C1 and angle is U+F515 in the [Calculator category mapping](https://github.com/microsoft/calculator/blob/main/src/Calculator.ViewModels/Common/NavCategory.cs).
- The imported Calculator font blob is `176efcb9900cc16fe0a923d0e66f9fe706d5f58c`. Outlines are stored as SVG paths; the font is not a runtime dependency.
- Unthemed SVGs retain source geometry. `.Delta`, `.Density`, `.Voltage`, `.Current`, and `.Resistance` variants reuse Fluent geometry with category-specific fills.

Attribution and license text are in [NOTICE.md](../../NOTICE.md).

## Validation

[`IconTests`](../../tests/UnitConverterExtension.Tests/IconTests.cs)
checks category selection, distinct colors within each theme, matching
light/dark geometry, and the `0 0 20 20` view box. It requires a contrast ratio
of at least 3:1 against `#F0F0F0` for light icons and `#333333` for dark icons.
Actual host backgrounds and selected-item rendering are outside those checks.