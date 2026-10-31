# Unit Converter - Supported Units

## Table of Contents
- [CSS and typography](#css-and-typography)
- [Acceleration](#acceleration)
- [Angle](#angle)
- [Area](#area)
- [Electric current](#electric-current)
- [Data](#data)
- [Data rate](#data-rate)
- [Density](#density)
- [Energy](#energy)
- [Force](#force)
- [Frequency](#frequency)
- [Fuel economy](#fuel-economy)
- [Length](#length)
- [Mass](#mass)
- [Power](#power)
- [Pressure](#pressure)
- [Electrical resistance](#electrical-resistance)
- [Speed](#speed)
- [Temperature](#temperature)
- [Temperature difference](#temperature-difference)
- [Time](#time)
- [Torque](#torque)
- [Voltage](#voltage)
- [Volume](#volume)

## CSS and typography

CSS absolute lengths use `px`, `pt`, `pc`, `Q`, `in`, `cm`, and `mm`.
Use `dtp pt` or `css pt` for CSS points and `dtp pica` or `css pc` for CSS picas.
`pt (DTP)`, `pt DTP`, `pt (CSS)`, and `pc (CSS)` are also accepted.
DTP means desktop publishing.

Bare `1 pt` keeps the preferred pint and offers `1 dtp pt` under Other interpretations.
Bare `1 pc` keeps parsec and offers `1 dtp pica` under Other interpretations.
For explicit CSS conversions, use the qualified spellings below.

| Input | Result |
|:--|:--|
| `16 px to dtp pt` | `12 pt (DTP)` |
| `12 css pt to px` | `16 px` |
| `1 css pc to px` | `16 px` |
| `4 Q to mm` | `1 mm` |
| `1 in to px` | `96 px` |
| `2.54 cm to px` | `96 px` |
| `25.4 mm to px` | `96 px` |

A CSS pixel is 1/96 inch; these conversions follow the [CSS absolute length definitions](https://www.w3.org/TR/css-values-4/#absolute-lengths) and do not depend on monitor DPI.
Printer points and picas use the traditional TeX sizes, which differ from DTP/CSS sizes.
Relative units such as `em`, `rem`, `%`, and viewport units require font or viewport context and are not supported.

## Acceleration

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`cm/s²` |  | Centimeter per second squared |  | ⭐ | cm/s2, cm/s^2, centimeter per second squared, centimeters per second squared, centimetre per second squared, centimetres per second squared |
`ft/s²` |  | Foot per second squared |  | ⭐ | ft/s2, ft/s^2, foot per second squared, feet per second squared |
`g0` | `g₀` | Standard gravity |  | ⭐ | standard gravity, standard gravities |
`in/s²` |  | Inch per second squared |  | ⭐ | in/s2, in/s^2, inch per second squared, inches per second squared |
`m/s²` |  | Meter per second squared |  | ⭐ | m/s2, m/s^2, meter per second squared, meters per second squared, metre per second squared, metres per second squared |
`mm/s²` |  | Millimeter per second squared |  | ⭐ | mm/s2, mm/s^2, millimeter per second squared, millimeters per second squared, millimetre per second squared, millimetres per second squared |

## Angle

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`deg` | `°` | Degree | Universal | ⭐ | degree, degrees, ° | 1/360 of a full rotation
`grad` |  | Gradian | Metric |  | gradian, gradians, gon | 1/400 of a full rotation
`rad` |  | Radian | Metric | ⭐ | radian, radians | SI unit of angular measure
`turn` |  | Turn | Universal |  | revolution, revolutions, rot | One complete rotation (360°)

## Area

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`acre` | `ac` | Acre | Imperial | ⭐ | acre, acres, ac | 43,560 square feet (approx. 4046.8564224 m²)
`cm2` | `cm²` | Square Centimeter | Metric | ⭐ | cm^2, cm², square centimeter, square centimetre, square centimeters, square centimetres, sq cm, sqcm | 1e-4 square meters
`ft2` | `ft²` | Square Foot | Imperial | ⭐ | ft^2, ft², square foot, square feet, sq ft, sqft | Area of a 1 ft by 1 ft square
`ha` |  | Hectare | Metric | ⭐ | hectare, hectares | 10,000 square meters
`in2` | `in²` | Square Inch | Imperial | ⭐ | in^2, in², square inch, square inches, sq in, sqin | Area of a 1 in by 1 in square
`km2` | `km²` | Square Kilometer | Metric | ⭐ | km^2, km², square kilometer, square kilometre, square kilometers, square kilometres, sq km, sqkm | 1,000,000 square meters
`m2` | `m²` | Square Meter | Metric | ⭐ | m^2, m², square meter, square metre, square meters, square metres, sq m, sqm | SI base unit of area
`manhattan block` | `Manhattan blk` | Manhattan City Block | Contextual |  | manhattan block, manhattan city block, nyc block, city block (manhattan) | Approx. 264 ft × 900 ft = 237,600 ft² ≈ 22,076 m²
`mi2` | `mi²` | Square Mile | Imperial |  | mi^2, mi², square mile, square miles, sq mi, sqmi | Area of a 1 mi by 1 mi square
`mm2` | `mm²` | Square Millimeter | Metric |  | mm^2, mm², square millimeter, square millimetre, square millimeters, square millimetres, sq mm, sqmm | 1e-6 square meters
`soccer field` | `soccer fld` | Soccer Field | Contextual |  | soccer field, soccer pitch, football pitch | Approx. FIFA-recommended pitch 105 m × 68 m = 7,140 m²
`yd2` | `yd²` | Square Yard | Imperial |  | yd^2, yd², square yard, square yards, sq yd, sqyd | Area of a 1 yd by 1 yd square

## Electric current

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`A` |  | Ampere |  | ⭐ | ampere, amperes, amp, amps |
`kA` |  | Kiloampere |  | ⭐ | kiloampere, kiloamperes, kiloamp, kiloamps |
`mA` |  | Milliampere |  | ⭐ | milliampere, milliamperes, milliamp, milliamps |
`MA` |  | Megaampere |  | ⭐ | megaampere, megaamperes, megaamp, megaamps |
`µA` |  | Microampere |  | ⭐ | microampere, microamperes, microamp, microamps, uA, μA |

## Data

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`b` |  | Bit | Digital | ⭐ | bit, bits | Basic unit of information
`B` |  | Byte | Digital | ⭐ | byte, bytes, octet, octets | 8 bits
`Eb` |  | Exabit | Digital |  | exabit, exabits, Ebit, Ebits | 1000 petabits (decimal)
`EB` |  | Exabyte | Digital |  | exabyte, exabytes | 1000 petabytes (decimal)
`Eib` |  | Exbibit | Digital |  | exbibit, exbibits | 1024 pebibits (binary)
`EiB` |  | Exbibyte | Digital |  | exbibyte, exbibytes | 1024 pebibytes (binary)
`Gb` |  | Gigabit | Digital | ⭐ | gigabit, gigabits, Gbit, Gbits | 1000 megabits (decimal)
`GB` |  | Gigabyte | Digital | ⭐ | gigabyte, gigabytes | 1000 megabytes (decimal)
`Gib` |  | Gibibit | Digital |  | gibibit, gibibits | 1024 mebibits (binary)
`GiB` |  | Gibibyte | Digital | ⭐ | gibibyte, gibibytes | 1024 mebibytes (binary)
`kb` |  | Kilobit | Digital | ⭐ | kilobit, kilobits, kbit, kbits | 1000 bits (decimal)
`kB` |  | Kilobyte | Digital | ⭐ | kilobyte, kilobytes | 1000 bytes (decimal)
`Kib` |  | Kibibit | Digital |  | kibibit, kibibits | 1024 bits (binary)
`KiB` |  | Kibibyte | Digital | ⭐ | kibibyte, kibibytes | 1024 bytes (binary)
`Mb` |  | Megabit | Digital | ⭐ | megabit, megabits, Mbit, Mbits | 1000 kilobits (decimal)
`MB` |  | Megabyte | Digital | ⭐ | megabyte, megabytes | 1000 kilobytes (decimal)
`Mib` |  | Mebibit | Digital |  | mebibit, mebibits | 1024 kibibits (binary)
`MiB` |  | Mebibyte | Digital | ⭐ | mebibyte, mebibytes | 1024 kibibytes (binary)
`nibble` |  | Nibble | Digital |  | nybble, nibbles, nybbles | 4 bits (half a byte)
`Pb` |  | Petabit | Digital |  | petabit, petabits, Pbit, Pbits | 1000 terabits (decimal)
`PB` |  | Petabyte | Digital |  | petabyte, petabytes | 1000 terabytes (decimal)
`Pib` |  | Pebibit | Digital |  | pebibit, pebibits | 1024 tebibits (binary)
`PiB` |  | Pebibyte | Digital |  | pebibyte, pebibytes | 1024 tebibytes (binary)
`Tb` |  | Terabit | Digital |  | terabit, terabits, Tbit, Tbits | 1000 gigabits (decimal)
`TB` |  | Terabyte | Digital | ⭐ | terabyte, terabytes | 1000 gigabytes (decimal)
`Tib` |  | Tebibit | Digital |  | tebibit, tebibits | 1024 gibibits (binary)
`TiB` |  | Tebibyte | Digital |  | tebibyte, tebibytes | 1024 gibibytes (binary)
`Yb` |  | Yottabit | Digital |  | yottabit, yottabits, Ybit, Ybits | 1000 zettabits (decimal)
`YB` |  | Yottabyte | Digital |  | yottabyte, yottabytes | 1000 zettabytes (decimal)
`YiB` |  | Yobibyte | Digital |  | yobibyte, yobibytes | 1024 zebibytes (binary)
`Zb` |  | Zettabit | Digital |  | zettabit, zettabits, Zbit, Zbits | 1000 exabits (decimal)
`ZB` |  | Zettabyte | Digital |  | zettabyte, zettabytes | 1000 exabytes (decimal)
`ZiB` |  | Zebibyte | Digital |  | zebibyte, zebibytes | 1024 exbibytes (binary)

## Data rate

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`b/s` |  | Bit per second | Digital | ⭐ | Bit per second, Bits per second, bps |
`B/s` |  | Byte per second | Digital | ⭐ | Byte per second, Bytes per second, Bps |
`Gb/s` |  | Gigabit per second | Digital | ⭐ | Gigabit per second, Gigabits per second, Gbps |
`GB/s` |  | Gigabyte per second | Digital | ⭐ | Gigabyte per second, Gigabytes per second, GBps |
`Gib/s` |  | Gibibit per second | Digital | ⭐ | Gibibit per second, Gibibits per second, Gibps |
`GiB/s` |  | Gibibyte per second | Digital | ⭐ | Gibibyte per second, Gibibytes per second, GiBps |
`kb/s` |  | Kilobit per second | Digital | ⭐ | Kilobit per second, Kilobits per second, kbps |
`kB/s` |  | Kilobyte per second | Digital | ⭐ | Kilobyte per second, Kilobytes per second, kBps |
`Kib/s` |  | Kibibit per second | Digital | ⭐ | Kibibit per second, Kibibits per second, Kibps |
`KiB/s` |  | Kibibyte per second | Digital | ⭐ | Kibibyte per second, Kibibytes per second, KiBps |
`Mb/s` |  | Megabit per second | Digital | ⭐ | Megabit per second, Megabits per second, Mbps |
`MB/s` |  | Megabyte per second | Digital | ⭐ | Megabyte per second, Megabytes per second, MBps |
`Mib/s` |  | Mebibit per second | Digital | ⭐ | Mebibit per second, Mebibits per second, Mibps |
`MiB/s` |  | Mebibyte per second | Digital | ⭐ | Mebibyte per second, Mebibytes per second, MiBps |
`Tb/s` |  | Terabit per second | Digital | ⭐ | Terabit per second, Terabits per second, Tbps |
`TB/s` |  | Terabyte per second | Digital | ⭐ | Terabyte per second, Terabytes per second, TBps |
`Tib/s` |  | Tebibit per second | Digital | ⭐ | Tebibit per second, Tebibits per second, Tibps |
`TiB/s` |  | Tebibyte per second | Digital | ⭐ | Tebibyte per second, Tebibytes per second, TiBps |

## Density

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`g/cm³` |  | Gram per cubic centimeter |  | ⭐ | g/cm3, g/cm^3, gram per cubic centimeter, grams per cubic centimeter, gram per cubic centimetre, grams per cubic centimetre |
`g/mL` |  | Gram per milliliter |  | ⭐ | gram per milliliter, grams per milliliter, gram per millilitre, grams per millilitre |
`kg/L` |  | Kilogram per liter |  | ⭐ | kilogram per liter, kilograms per liter, kilogram per litre, kilograms per litre |
`kg/m³` |  | Kilogram per cubic meter |  | ⭐ | kg/m3, kg/m^3, kilogram per cubic meter, kilograms per cubic meter, kilogram per cubic metre, kilograms per cubic metre |
`lb/ft³` |  | Pound per cubic foot |  | ⭐ | lb/ft3, lb/ft^3, pound per cubic foot, pounds per cubic foot |
`lb/in³` |  | Pound per cubic inch |  | ⭐ | lb/in3, lb/in^3, pound per cubic inch, pounds per cubic inch |

## Energy

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`BTU` |  | British Thermal Unit | Imperial | ⭐ | btu, british thermal unit, british thermal units | Traditional unit of heat energy
`cal` |  | Calorie | Metric | ⭐ | small calorie, small calories, thermochemical calorie, thermochemical calories, gram calorie, gram calories, cal-th, cal th | Thermochemical calorie (4.184 J)
`eV` |  | Electron Volt | Physics |  | electron volt, electron volts | Elementary charge times one volt
`ft·lb` |  | Foot-pound | Imperial | ⭐ | ft-lb, ft lb, foot-pound, foot-pounds, lbf·ft, lb-ft | Imperial unit of energy
`J` |  | Joule | Metric | ⭐ | joule, joules | SI unit of energy
`kcal` |  | Kilocalorie | Metric | ⭐ | kilocalorie, kilocalories, food calorie, food calories, large calorie, large calories | 1000 calories, commonly used for food energy
`kJ` |  | Kilojoule | Metric | ⭐ | kilojoule, kilojoules | 1000 joules
`kWh` |  | Kilowatt-hour | Metric | ⭐ | kilowatt-hour, kilowatt-hours, kW·h, kW h | Unit of electrical energy consumption

## Force

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`dyn` |  | Dyne |  | ⭐ | dyne, dynes |
`kgf` |  | Kilogram-force |  | ⭐ | kilogram-force, kilograms-force, kilogram force, kilograms force |
`kN` |  | Kilonewton |  | ⭐ | kilonewton, kilonewtons |
`lbf` |  | Pound-force |  | ⭐ | pound-force, pounds-force, pound force, pounds force |
`mN` |  | Millinewton |  | ⭐ | millinewton, millinewtons |
`MN` |  | Meganewton |  | ⭐ | meganewton, meganewtons |
`N` |  | Newton |  | ⭐ | newton, newtons |
`µN` |  | Micronewton |  | ⭐ | micronewton, micronewtons, uN, μN |

## Frequency

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`bpm` |  | Beat per minute |  | ⭐ | beat per minute, beats per minute |
`GHz` |  | Gigahertz |  | ⭐ | gigahertz, ghz |
`Hz` |  | Hertz |  | ⭐ | hertz, hz, cycle per second, cycles per second, rps, revolution per second, revolutions per second, 1/s, s^-1, s⁻¹ |
`kHz` |  | Kilohertz |  | ⭐ | kilohertz, khz |
`mHz` |  | Millihertz |  | ⭐ | millihertz |
`MHz` |  | Megahertz |  | ⭐ | megahertz |
`rad/s` |  | Radian per second |  | ⭐ | radian per second, radians per second |
`rpm` |  | Revolution per minute |  | ⭐ | revolution per minute, revolutions per minute, cycle per minute, cycles per minute, cpm |
`THz` |  | Terahertz |  | ⭐ | terahertz, thz |
`µHz` |  | Microhertz |  | ⭐ | microhertz, uHz, μHz |

## Fuel economy

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`km/L` |  | Kilometer per liter |  | ⭐ | kilometer per liter, kilometers per liter, kilometre per litre, kilometres per litre |
`L/100km` |  | Liter per 100 kilometers |  | ⭐ | L/100 km, liter per 100 kilometers, liters per 100 kilometers, litre per 100 kilometres, litres per 100 kilometres |
`mpg UK` |  | Mile per imperial gallon |  | ⭐ | UK mpg, mpg (UK), imperial mpg, mpg imperial, mile per UK gallon, miles per UK gallon, mile per imperial gallon, miles per imperial gallon |
`mpg US` |  | Mile per US gallon |  | ⭐ | US mpg, mpg (US), mile per US gallon, miles per US gallon |

## Length

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`angstrom` | `Å` | Angstrom | Non-SI |  | Angstrom, angstroms, å |
`au` | `AU` | Astronomical Unit | Astronomical |  | Astronomical Unit, ua, astronomical units |
`cm` |  | Centimeter | Metric | ⭐ | Centimeter, centimetre, centimetres, centimeters |
`dam` |  | Decameter | SI |  | Decameter, decametre, dekameter, dekametre, decameters, decametres |
`dm` |  | Decimeter | SI |  | Decimeter, decimetre, decimeters, decimetres |
`dmi` |  | Data Mile | Customary |  | Data Mile, data miles |
`dtp pica` | `pica (DTP)` | DTP Pica | Typographic |  | Dtp Pica, dtp picas, pica, picas, pica DTP, css pc, pc (CSS) | 1/6 inch; 12 DTP points (CSS pica)
`dtp pt` | `pt (DTP)` | DTP Point | Typographic |  | Dtp Point, dtp points, dtp pt, point, points, pt DTP, css pt, pt (CSS) | 1/72 inch (CSS point)
`fathom` | `ftm` | Fathom | Imperial |  | Fathom, fathoms |
`fm` |  | Femtometer | SI |  | Femtometer, femtometre, femtometers, femtometres, fermi |
`ft` |  | Foot | Imperial | ⭐ | Foot, ', ′, feet |
`Gm` |  | Gigameter | SI |  | Gigameter, gigametre, gigameters, gigametres |
`hand` |  | Hand | Imperial |  | Hand, hands |
`hm` |  | Hectometer | SI |  | Hectometer, hectometre, hectometers, hectometres |
`ch` |  | Chain | Imperial |  | Chain, chains |
`in` |  | Inch | Imperial | ⭐ | Inch, ", ″, inches |
`kft` |  | Kilofoot | Imperial |  | Kilofoot, k', k′, kilofeet |
`kly` |  | Kilolight Year | Astronomical |  | Kilolight Year, kilolightyear, kilolightyears, kilo light year, kilo light years |
`km` |  | Kilometer | Metric | ⭐ | Kilometer, kilometre, kilometres, kilometers, klick, klicks, click, clicks |
`kpc` |  | Kiloparsec | Astronomical |  | Kiloparsec, kiloparsecs |
`kyd` |  | Kiloyard | Imperial |  | Kiloyard, kiloyards |
`ld` |  | Light-day | Astronomical |  | light day, light days, light-day, light-days | Distance light travels in one day
`lh` |  | Light-hour | Astronomical |  | light hour, light hours, light-hour, light-hours | Distance light travels in one hour
`lmin` |  | Light-minute | Astronomical |  | light minute, light minutes, light-minute, light-minutes | Distance light travels in one minute
`ls` |  | Light-second | Astronomical |  | light second, light seconds, light-second, light-seconds | Distance light travels in one second
`lw` |  | Light-week | Astronomical |  | light week, light weeks, light-week, light-weeks | Distance light travels in one week
`ly` |  | Light Year | Astronomical |  | Light Year, light years, lightyear, lightyears |
`m` |  | Meter | Metric | ⭐ | Meter, metre, metres, meters |
`mi` |  | Mile | Imperial | ⭐ | Mile, miles |
`microinch` | `µin` | Microinch | Imperial |  | Microinch, microinches, µin, uin |
`mil` |  | Mil | Imperial |  | Mil, thou |
`Mly` |  | Megalight Year | Astronomical |  | Megalight Year, megalightyear, megalightyears, mega light year, mega light years |
`mm` |  | Millimeter | Metric | ⭐ | Millimeter, millimetre, millimetres, millimeters |
`Mm` |  | Megameter | SI |  | Megameter, megametre, megameters, megametres |
`Mpc` |  | Megaparsec | Astronomical |  | Megaparsec, megaparsecs |
`nm` |  | Nanometer | Metric | ⭐ | Nanometer, nanometre, nanometres, nanometers |
`nmi` |  | Nautical Mile | Nautical |  | Nautical Mile, nautical miles |
`pc` |  | Parsec | Astronomical |  | Parsec, parsecs |
`pm` |  | Picometer | SI |  | Picometer, picometre, picometers, picometres |
`printer pica` | `pica (printer)` | Printer Pica | Typographic |  | Printer Pica, printer picas | 12 printer points; 400/2409 inch (TeX pica)
`printer pt` | `pt (printer)` | Printer Point | Typographic |  | Printer Point, printer points, pt printer | 1/72.27 inch (TeX point)
`px` |  | CSS Pixel | Typographic |  | pixel, pixels, css pixel, css pixels | 1/96 inch (CSS reference pixel, independent of device pixel density)
`Q` |  | Quarter-millimeter | Typographic |  | q, quarter-millimeter, quarter-millimeters, quarter-millimetre, quarter-millimetres | 1/4 millimeter (CSS quarter-millimeter)
`R⊙` |  | Solar Radius |  |  | Solar Radius |
`shackle` |  | Shackle | Nautical |  | Shackle, shackles |
`twip` |  | Twip | Typographic |  | Twip, twips | 1/20 DTP point; 1/1440 inch
`um` | `µm` | Micrometer | Metric | ⭐ | Micrometer, micrometre, micrometres, micrometers, um |
`us survey ft` | `US ft (survey)` | Us Survey Foot | Imperial |  | Us Survey Foot, us survey feet, us ft (survey), survey foot, survey feet |
`yd` |  | Yard | Imperial | ⭐ | Yard, yards |

## Mass

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`cg` |  | Centigram | Metric |  | centigram, centigrams | One hundredth of a gram
`ct` |  | Carat | Metric |  | carat, carats | Unit for measuring precious stones and pearls (200 mg)
`dag` |  | Dekagram | Metric |  | dekagram, dekagrams, decagram, decagrams | Ten grams
`dg` |  | Decigram | Metric |  | decigram, decigrams | One tenth of a gram
`g` |  | Gram | Metric | ⭐ | gram, grams | Base unit of mass in CGS system
`hg` |  | Hectogram | Metric |  | hectogram, hectograms | One hundred grams
`kg` |  | Kilogram | Metric | ⭐ | kilogram, kilograms | SI base unit of mass
`lb` |  | Pound | Imperial | ⭐ | pound, pounds, lbs | 16 ounces
`mg` |  | Milligram | Metric | ⭐ | milligram, milligrams | One thousandth of a gram
`oz` |  | Ounce | Imperial | ⭐ | ounce, ounces | 1/16 of a pound
`short tn` |  | Short Ton | US | ⭐ | short ton, short tons, US ton, US tons, ton, tons | 2000 pounds (US)
`stone` | `st` | Stone | Imperial |  | stone, stones | 14 pounds (UK/Ireland)
`t` |  | Metric Tonne | Metric | ⭐ | tonne, tonnes, metric ton, metric tons | 1000 kilograms

## Power

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`BTU/min` |  | British Thermal Unit per minute | Imperial |  | btu/min, british thermal units per minute, british thermal unit per minute | Thermal power unit
`ft·lb/min` |  | Foot-pound per minute | Imperial |  | ft-lb/min, foot-pounds per minute, foot-pound per minute | Imperial unit of power
`hp` |  | Horsepower | Imperial | ⭐ | horsepower, horsepower (us), hp(us), mechanical horsepower | Mechanical horsepower (745.7 watts)
`kW` |  | Kilowatt | Metric | ⭐ | kilowatt, kilowatts | 1000 watts
`W` |  | Watt | Metric | ⭐ | watt, watts | SI unit of power

## Pressure

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`atm` |  | Atmosphere | Physics | ⭐ | atmosphere, atmospheres | Standard atmospheric pressure
`bar` |  | Bar | Metric | ⭐ | bars | 100,000 pascals
`inHg` |  | Inch of mercury | Imperial |  | inches of mercury, in Hg | Pressure equivalent to 1 inch of mercury
`kPa` |  | Kilopascal | Metric | ⭐ | kilopascal, kilopascals | 1000 pascals
`mmHg` |  | Millimeter of mercury | Medical | ⭐ | millimeters of mercury, mm Hg, torr | Pressure equivalent to 1mm of mercury
`Pa` |  | Pascal | Metric | ⭐ | pascal, pascals | SI unit of pressure
`psi` |  | Pound per square inch | Imperial | ⭐ | pounds per square inch, pound per square inch | Pressure per square inch

## Electrical resistance

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`kohm` | `kΩ` | Kiloohm |  | ⭐ | kiloohm, kiloohms, kilohm, kilohms |
`mohm` | `mΩ` | Milliohm |  | ⭐ | milliohm, milliohms |
`Mohm` | `MΩ` | Megaohm |  | ⭐ | megaohm, megaohms, megohm, megohms |
`ohm` | `Ω` | Ohm |  | ⭐ | ohm, ohms |
`uohm` | `µΩ` | Microohm |  | ⭐ | microohm, microohms, uΩ, μΩ |

## Speed

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`cm/s` |  | Centimeter per second | Metric |  | centimeters per second, centimetres per second, cmps |
`ft/s` |  | Foot per second | Imperial | ⭐ | feet per second, foot per second, fps |
`km/h` |  | Kilometer per hour | Metric | ⭐ | kilometers per hour, kilometres per hour, kph, kmh |
`kn` |  | Knot | Nautical | ⭐ | knot, knots, kt, kts |
`m/s` |  | Meter per second | Metric | ⭐ | meter per second, meters per second, metre per second, metres per second, mps |
`Mach` |  | Mach number | Physics |  | mach |
`min/km` |  | Minute per kilometer | Metric | ⭐ | minute per kilometer, minutes per kilometer, minute per kilometre, minutes per kilometre |
`min/mi` |  | Minute per mile | Imperial | ⭐ | min/mile, minute per mile, minutes per mile |
`mph` |  | Mile per hour | Imperial | ⭐ | miles per hour |
`s/km` |  | Second per kilometer | Metric | ⭐ | sec/km, second per kilometer, seconds per kilometer, second per kilometre, seconds per kilometre |
`s/mi` |  | Second per mile | Imperial | ⭐ | s/mile, sec/mi, second per mile, seconds per mile |

## Temperature

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`°C` |  | Celsius | Metric | ⭐ | celsius, c, centigrade | Temperature scale based on water's freezing and boiling points
`°F` |  | Fahrenheit | Imperial | ⭐ | fahrenheit, f | Temperature scale commonly used in the United States
`°R` |  | Rankine | Imperial |  | rankine, r | Absolute temperature scale based on Fahrenheit degrees
`K` |  | Kelvin | Metric |  | kelvin, k | Absolute temperature scale (SI base unit)

## Temperature difference

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`Δ°C` |  | Celsius difference |  | ⭐ | ΔC, ∆°C, delta C, delta °C, delta Celsius, Celsius difference, Celsius differences |
`Δ°F` |  | Fahrenheit difference |  | ⭐ | ΔF, ∆°F, delta F, delta °F, delta Fahrenheit, Fahrenheit difference, Fahrenheit differences |
`Δ°R` |  | Rankine difference |  | ⭐ | ΔR, ∆°R, delta R, delta °R, delta Rankine, Rankine difference, Rankine differences |
`ΔK` |  | Kelvin difference |  | ⭐ | ∆K, delta K, delta Kelvin, Kelvin difference, Kelvin differences |

## Time

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`d` |  | Day | Universal | ⭐ | day, days | 24 hours
`h` |  | Hour | Universal | ⭐ | hr, hrs, hour, hours | 60 minutes
`min` |  | Minute | Universal | ⭐ | mins, minute, minutes | 60 seconds
`mo` |  | Month | Universal | ⭐ | month, months | Approximately 30.44 days
`ms` |  | Millisecond | Metric | ⭐ | millisecond, milliseconds | One thousandth of a second
`ns` |  | Nanosecond | Metric |  | nanosecond, nanoseconds | One billionth of a second
`s` |  | Second | Metric | ⭐ | sec, second, seconds | SI base unit of time
`w` |  | Week | Universal | ⭐ | wk, week, weeks | 7 days
`y` |  | Year | Universal | ⭐ | yr, year, years | 365.25 days (Julian year)
`μs` |  | Microsecond | Metric |  | microsecond, microseconds, us | One millionth of a second

## Torque

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`kgf·m` |  | Kilogram-force meter |  | ⭐ | kgf m, kgf*m, kgf-m, kilogram-force meter, kilogram-force meters, kilogram-force metre, kilogram-force metres |
`kNm` | `kN·m` | Kilonewton meter |  | ⭐ | kN m, kN*m, kilonewton meter, kilonewton meters, kilonewton metre, kilonewton metres |
`lbf·ft` |  | Pound-force foot |  | ⭐ | lbf ft, lbf*ft, lbf-ft, pound-force foot, pound-force feet |
`lbf·in` |  | Pound-force inch |  | ⭐ | lbf in, lbf*in, lbf-in, pound-force inch, pound-force inches |
`Ncm` | `N·cm` | Newton centimeter |  | ⭐ | N cm, N*cm, newton centimeter, newton centimeters, newton centimetre, newton centimetres |
`Nm` | `N·m` | Newton meter |  | ⭐ | N m, N*m, newton meter, newton meters, newton metre, newton metres, newton-meter, newton-meters |
`Nmm` | `N·mm` | Newton millimeter |  | ⭐ | N mm, N*mm, newton millimeter, newton millimeters, newton millimetre, newton millimetres |

## Voltage

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`kV` |  | Kilovolt |  | ⭐ | kilovolt, kilovolts |
`mV` |  | Millivolt |  | ⭐ | millivolt, millivolts |
`MV` |  | Megavolt |  | ⭐ | megavolt, megavolts |
`V` |  | Volt |  | ⭐ | volt, volts, v |
`µV` |  | Microvolt |  | ⭐ | microvolt, microvolts, uV, μV |

## Volume

Unit | Symbol | Name | System | Common | Aliases | Description
:--|:--|:--|:--|:-:|:--|:--
`cup` |  | Cup | US | ⭐ | cups | 8 US fluid ounces
`imp fl oz` |  | Imperial Fluid Ounce | Imperial |  | imperial fluid ounce, imperial fluid ounces | 1/20 imperial pint
`imp gal` |  | Imperial Gallon | Imperial |  | imperial gallon, imperial gallons | 4 imperial quarts
`imp pt` |  | Imperial Pint | Imperial |  | imperial pint, imperial pints | 20 imperial fluid ounces
`imp qt` |  | Imperial Quart | Imperial |  | imperial quart, imperial quarts | 2 imperial pints
`l` | `L` | Liter | Metric | ⭐ | liter, litre, liters, litres | Basic metric unit of volume
`ml` | `mL` | Milliliter | Metric | ⭐ | milliliter, millilitre, milliliters, millilitres | One thousandth of a liter
`tbsp` |  | Tablespoon | US | ⭐ | tablespoon, tablespoons | 3 teaspoons (US)
`tsp` |  | Teaspoon | US | ⭐ | teaspoon, teaspoons | 1/3 tablespoon (US)
`us fl oz` | `US fl oz` | US Fluid Ounce | US | ⭐ | us fluid ounce, us fluid ounces, us fl. oz, us fl.oz, us floz | 1/8 US cup
`us gal` | `US gal` | US Gallon | US | ⭐ | us gallon, us gallons | 4 US quarts
`us pt` | `US pt` | US Pint | US | ⭐ | us pint, us pints | 2 US cups
`us qt` | `US qt` | US Quart | US | ⭐ | us quart, us quarts | 2 US pints

---
**Legend:**
- ⭐ = Commonly used unit
- System = Unit system (Metric, Imperial, etc.)
- Symbol = Display symbol if different from ID
