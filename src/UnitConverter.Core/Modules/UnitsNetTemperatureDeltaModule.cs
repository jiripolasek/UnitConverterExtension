using UnitsNet;
using UnitsNet.Units;

namespace JPSoftworks.UnitConverter.Core.Modules;

internal sealed class UnitsNetTemperatureDeltaModule : DescriptorBasedConversionModule
{
    private static readonly Dictionary<string, TemperatureDeltaUnit> UnitMap = new(StringComparer.Ordinal)
    {
        ["Δ°C"] = TemperatureDeltaUnit.DegreeCelsius,
        ["Δ°F"] = TemperatureDeltaUnit.DegreeFahrenheit,
        ["ΔK"] = TemperatureDeltaUnit.Kelvin,
        ["Δ°R"] = TemperatureDeltaUnit.DegreeRankine
    };

    public UnitsNetTemperatureDeltaModule() : base(CreateDescriptors()) { }

    public override string Category => "TemperatureDelta";
    public override bool SupportsSquaredUnits => false;
    public override bool SupportsCubicUnits => false;
    protected override UnitType GetUnitType() => UnitType.TemperatureDelta;

    public override bool TryConvert(double value, string fromUnit, string toUnit, out double result)
    {
        result = 0;
        if (!double.IsFinite(value) ||
            !TryNormalizeUnit(fromUnit, CultureInfo.InvariantCulture, out var from) ||
            !TryNormalizeUnit(toUnit, CultureInfo.InvariantCulture, out var to))
            return false;

        try
        {
            result = TemperatureDelta.From(value, UnitMap[from]).As(UnitMap[to]);
            return double.IsFinite(result);
        }
        catch (ArgumentException) { return false; }
    }

    private static IEnumerable<UnitDescriptor> CreateDescriptors()
    {
        UnitDescriptor[] descriptors =
        [
            new()
            {
                Id = "Δ°C", DisplayName = "Celsius difference",
                Aliases = ["ΔC", "∆°C", "delta C", "delta °C", "delta Celsius", "Celsius difference", "Celsius differences"]
            },
            new()
            {
                Id = "Δ°F", DisplayName = "Fahrenheit difference",
                Aliases = ["ΔF", "∆°F", "delta F", "delta °F", "delta Fahrenheit", "Fahrenheit difference", "Fahrenheit differences"]
            },
            new()
            {
                Id = "ΔK", DisplayName = "Kelvin difference",
                Aliases = ["∆K", "delta K", "delta Kelvin", "Kelvin difference", "Kelvin differences"]
            },
            new()
            {
                Id = "Δ°R", DisplayName = "Rankine difference",
                Aliases = ["ΔR", "∆°R", "delta R", "delta °R", "delta Rankine", "Rankine difference", "Rankine differences"]
            }
        ];
        return descriptors.Select(unit => unit with
        {
            Symbol = unit.Id,
            CommonTargets = UnitMap.Keys.Where(target => target != unit.Id).ToArray()
        });
    }
}