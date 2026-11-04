namespace SimuLearn.Domain.Simulations.Models;

/// <summary>
/// MVP physics model: a simple DC circuit with one ohmic resistor. Current and power are
/// calculated with Ohm's law (I = V / R) and the power relation (P = V I).
/// </summary>
public sealed class DcCircuitModel : SimulationModelBase
{
    public const string Key = "simple-dc-circuit-v1";

    public override string ModelKey => Key;

    public override string Domain => "Electricity and magnetism";

    public override string Title => "Current in a simple DC circuit";

    public override string LearningObjective =>
        "Understand how source voltage and resistance determine current in a simple series circuit " +
        "(Ohm's law), and why doubling resistance halves the current at constant voltage.";

    public override string LearningObjectiveCode => "PHY-ELEC-OHMS-LAW";

    public override string GoverningRelationship => "I = V / R (Ohm's law)";

    protected override IReadOnlyList<ParameterDefinition> ParameterDefinitions { get; } =
    [
        new("voltageV", "Source voltage", 0, 24, 0.5, "V", 12, "V"),
        new("resistanceOhm", "Resistance", 1, 100, 0.5, "Ω", 6, "R"),
    ];

    public override IReadOnlyList<string> Keywords { get; } =
    [
        "circuit", "ohm", "ohms", "voltage", "volt", "resistance", "resistor", "current",
        "battery", "amps", "amperes", "electricity", "electrical",
    ];

    public override IReadOnlyList<ConceptNote> Concepts { get; } =
    [
        new("Voltage", "The electrical potential difference that pushes charge around the circuit, measured in volts (V)."),
        new("Resistance", "How strongly a component opposes current, measured in ohms (Ω)."),
        new("Current", "The rate of charge flow through the circuit, measured in amperes (A)."),
        new("Power", "The energy transferred each second by the resistor, measured in watts (W); P = V × I."),
    ];

    public override IReadOnlyList<string> Features { get; } =
        ["live-meter-readings", "current-flow-animation", "sweep-comparison", "measurement-panel"];

    public override IReadOnlyList<string> Assumptions { get; } =
    [
        "The source is an ideal DC voltage source with no internal resistance.",
        "The resistor is ohmic, so its resistance does not change with temperature or current.",
        "The circuit is a single closed series loop at steady state.",
    ];

    public override IReadOnlyList<string> Limitations { get; } =
    [
        "The model covers a single resistor only; series and parallel networks are out of scope for this template.",
        "Transient behaviour from inductance or capacitance is not simulated.",
        "Power is reported as heat dissipated by the resistor; real components may have different limits.",
    ];

    protected override SimulationOutcome Compute(ParameterSet state)
    {
        var voltage = state["voltageV"];
        var resistance = state["resistanceOhm"];

        var current = resistance > 0 ? voltage / resistance : 0d;
        var power = voltage * current;
        var conductance = resistance > 0 ? 1d / resistance : 0d;

        var observations = new List<SimulationObservation>
        {
            new("current", "Current", current, "A", "I"),
            new("power", "Power dissipated by the resistor", power, "W", "P"),
            new("voltageDrop", "Voltage across the resistor", voltage, "V", "V"),
            new("conductance", "Conductance", conductance, "S", "G"),
        };

        var summary =
            $"A {FormatTrim(voltage)} V source across a {FormatTrim(resistance)} Ω resistor drives " +
            $"{Format(current)} A of current and dissipates {Format(power)} W.";

        var grounding = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            ["voltageV"] = voltage,
            ["resistanceOhm"] = resistance,
            ["current"] = current,
            ["power"] = power,
            ["conductance"] = conductance,
        };

        return new SimulationOutcome
        {
            Observations = observations,
            Trajectory = [],
            Summary = summary,
            IsInMotion = false,
            Warnings = [],
            GroundingValues = grounding,
        };
    }

    public override string ExplainChange(
        ParameterSet before,
        SimulationOutcome beforeOutcome,
        ParameterSet after,
        SimulationOutcome afterOutcome)
    {
        var parts = new List<string>();

        var voltageChanged = Changed(before, after, "voltageV");
        var resistanceChanged = Changed(before, after, "resistanceOhm");

        var previousCurrent = beforeOutcome.Find("current")?.Value ?? 0d;
        var currentCurrent = afterOutcome.Find("current")?.Value ?? 0d;
        var previousVoltage = before["voltageV"];
        var currentVoltage = after["voltageV"];
        var previousResistance = before["resistanceOhm"];
        var currentResistance = after["resistanceOhm"];

        if (voltageChanged)
        {
            parts.Add($"The source voltage changed from {FormatTrim(previousVoltage)} V to {FormatTrim(currentVoltage)} V.");
        }

        if (resistanceChanged)
        {
            parts.Add($"The resistance changed from {FormatTrim(previousResistance)} Ω to {FormatTrim(currentResistance)} Ω.");
        }

        if (voltageChanged && !resistanceChanged && previousVoltage > 0)
        {
            var factor = currentVoltage / previousVoltage;
            var (voltageWord, currentWord) = DescribeDirect(factor);
            parts.Add(
                $"At constant resistance, current is proportional to voltage: {voltageWord} the voltage " +
                $"{currentWord} the current, from {Format(previousCurrent)} A to {Format(currentCurrent)} A.");
        }
        else if (resistanceChanged && !voltageChanged && previousResistance > 0)
        {
            var factor = currentResistance / previousResistance;
            var (resistanceWord, currentWord) = DescribeInverse(factor);
            parts.Add(
                $"At constant voltage, current is inversely proportional to resistance: {resistanceWord} the " +
                $"resistance {currentWord} the current, from {Format(previousCurrent)} A to {Format(currentCurrent)} A.");
        }
        else
        {
            parts.Add($"Using I = V / R, the current is now {Format(currentCurrent)} A.");
        }

        parts.Add(
            $"The resistor dissipates {Format(afterOutcome.Find("power")?.Value ?? 0d)} W of power at " +
            $"{FormatTrim(currentVoltage)} V and {FormatTrim(currentResistance)} Ω.");

        parts.Add(
            "This model assumes an ideal source, a single ohmic resistor and steady-state DC, so it is a " +
            "simplified representation of a real circuit.");

        return string.Join(" ", parts);
    }

    private static (string ResistanceWord, string CurrentWord) DescribeInverse(double resistanceRatio)
    {
        if (Nearly(resistanceRatio, 2))
        {
            return ("doubling", "halves");
        }

        if (Nearly(resistanceRatio, 0.5))
        {
            return ("halving", "doubles");
        }

        return (
            $"multiplying the resistance by {FormatTrim(resistanceRatio)}",
            $"divides the current by {FormatTrim(resistanceRatio)}");
    }

    private static (string VoltageWord, string CurrentWord) DescribeDirect(double voltageRatio)
    {
        if (Nearly(voltageRatio, 2))
        {
            return ("doubling", "doubles");
        }

        if (Nearly(voltageRatio, 0.5))
        {
            return ("halving", "halves");
        }

        return (
            $"multiplying the voltage by {FormatTrim(voltageRatio)}",
            $"multiplies the current by {FormatTrim(voltageRatio)}");
    }

    private static bool Nearly(double left, double right) => Math.Abs(left - right) < 1e-3;

    private static bool Changed(ParameterSet before, ParameterSet after, string name) =>
        before.TryGet(name, out var previous)
        && after.TryGet(name, out var current)
        && Math.Abs(current - previous) > ComparisonTolerance;
}
