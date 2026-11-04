using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Simulations;

/// <summary>
/// A deterministic, server-authoritative simulation model. Implementations own the physics and
/// parameter bounds; no language model output is ever executed as code (SRS 4.3, SEC risks).
/// </summary>
public interface ISimulationModel
{
    string ModelKey { get; }

    string SchemaVersion { get; }

    string Subject { get; }

    string Domain { get; }

    string Title { get; }

    string LearningObjective { get; }

    string LearningObjectiveCode { get; }

    string Renderer { get; }

    /// <summary>The validated equation or rule set the implementation uses.</summary>
    string GoverningRelationship { get; }

    IReadOnlyList<ParameterDefinition> Parameters { get; }

    /// <summary>Terms this model can route a natural-language request to (used by the fallback interpreter).</summary>
    IReadOnlyList<string> Keywords { get; }

    /// <summary>Short, curriculum-safe definitions available to the tutor.</summary>
    IReadOnlyList<ConceptNote> Concepts { get; }

    IReadOnlyList<string> Features { get; }

    IReadOnlyList<string> Assumptions { get; }

    IReadOnlyList<string> Limitations { get; }

    ParameterSet CreateInitialState();

    /// <summary>Fills omitted parameters with their defaults and drops unknown keys.</summary>
    ParameterSet Normalize(ParameterSet state);

    SimulationValidationResult Validate(ParameterSet state);

    SimulationOutcome Simulate(ParameterSet state);

    /// <summary>Deterministic explanation of what changed between two states of this model.</summary>
    string ExplainChange(ParameterSet before, SimulationOutcome beforeOutcome, ParameterSet after, SimulationOutcome afterOutcome);
}

public abstract class SimulationModelBase : ISimulationModel
{
    protected const double ComparisonTolerance = 1e-9;

    public abstract string ModelKey { get; }

    public virtual string SchemaVersion => "1.0.0";

    public virtual string Subject => "Physics";

    public abstract string Domain { get; }

    public abstract string Title { get; }

    public abstract string LearningObjective { get; }

    public abstract string LearningObjectiveCode { get; }

    public virtual string Renderer => "svg-2d";

    public abstract string GoverningRelationship { get; }

    protected abstract IReadOnlyList<ParameterDefinition> ParameterDefinitions { get; }

    public IReadOnlyList<ParameterDefinition> Parameters => ParameterDefinitions;

    public abstract IReadOnlyList<string> Keywords { get; }

    public virtual IReadOnlyList<ConceptNote> Concepts => [];

    public abstract IReadOnlyList<string> Features { get; }

    public abstract IReadOnlyList<string> Assumptions { get; }

    public abstract IReadOnlyList<string> Limitations { get; }

    public ParameterSet CreateInitialState() =>
        new(ParameterDefinitions.Select(p => KeyValuePair.Create(p.Name, p.Default)));

    public ParameterSet Normalize(ParameterSet state)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in ParameterDefinitions)
        {
            var value = state.TryGet(parameter.Name, out var supplied) && IsFinite(supplied)
                ? supplied
                : parameter.Default;
            values[parameter.Name] = Math.Clamp(value, parameter.Min, parameter.Max);
        }

        return new ParameterSet(values);
    }

    public SimulationValidationResult Validate(ParameterSet state)
    {
        var issues = new List<ParameterIssue>();
        foreach (var (name, value) in state.Values)
        {
            var parameter = ParameterDefinitions.FirstOrDefault(
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            if (parameter is null)
            {
                issues.Add(new ParameterIssue(
                    name,
                    $"Parameter '{name}' is not supported by simulation '{ModelKey}'.",
                    "unknown_parameter"));
                continue;
            }

            if (!IsFinite(value))
            {
                issues.Add(new ParameterIssue(parameter.Name, $"{parameter.Label} must be a finite number.", "not_numeric"));
                continue;
            }

            if (!parameter.Contains(value))
            {
                issues.Add(new ParameterIssue(
                    parameter.Name,
                    $"{parameter.Label} must be between {ParameterSet.Format(parameter.Min)} and " +
                    $"{ParameterSet.Format(parameter.Max)} {parameter.Unit}.",
                    "out_of_range"));
                continue;
            }

            if (parameter.HasIntegralValuesOnly && Math.Abs(value % 1d) > ComparisonTolerance)
            {
                issues.Add(new ParameterIssue(
                    parameter.Name,
                    $"{parameter.Label} must be a whole number of {parameter.Unit}.",
                    "invalid_step"));
            }
        }

        return issues.Count == 0 ? SimulationValidationResult.Valid : new SimulationValidationResult(issues);
    }

    public SimulationOutcome Simulate(ParameterSet state)
    {
        var validation = Validate(state);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                $"The state is not valid for simulation '{ModelKey}'.",
                validation.ToErrors());
        }

        var outcome = Compute(Normalize(state));
        return EnrichGrounding(outcome);
    }

    protected abstract SimulationOutcome Compute(ParameterSet state);

    public virtual string ExplainChange(
        ParameterSet before,
        SimulationOutcome beforeOutcome,
        ParameterSet after,
        SimulationOutcome afterOutcome)
    {
        var changes = new List<string>();
        foreach (var parameter in Parameters)
        {
            if (!before.TryGet(parameter.Name, out var previous) || !after.TryGet(parameter.Name, out var current))
            {
                continue;
            }

            if (Math.Abs(current - previous) <= ComparisonTolerance)
            {
                continue;
            }

            changes.Add(
                $"{parameter.Label} changed from {ParameterSet.Format(previous)} {parameter.Unit} " +
                $"to {ParameterSet.Format(current)} {parameter.Unit}");
        }

        if (changes.Count == 0)
        {
            return "The settings are unchanged, so the calculated result is unchanged.";
        }

        return $"{Capitalise(string.Join("; ", changes))}. Using {GoverningRelationship}, the model now reports: " +
               afterOutcome.Summary;
    }

    /// <summary>
    /// Makes every value the model legitimately uses available to the explanation-grounding guard.
    /// </summary>
    private SimulationOutcome EnrichGrounding(SimulationOutcome outcome)
    {
        var values = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var observation in outcome.Observations)
        {
            values[observation.Name] = observation.Value;
        }

        foreach (var (key, value) in outcome.GroundingValues)
        {
            values[key] = value;
        }

        return new SimulationOutcome
        {
            Observations = outcome.Observations,
            Trajectory = outcome.Trajectory,
            Summary = outcome.Summary,
            IsInMotion = outcome.IsInMotion,
            Warnings = outcome.Warnings,
            GroundingValues = values,
        };
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static string Capitalise(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];

    protected static string Format(double value, int decimals = 2) =>
        value.ToString($"F{decimals}", System.Globalization.CultureInfo.InvariantCulture);

    protected static string FormatTrim(double value) =>
        value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}
