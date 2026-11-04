using SimuLearn.Domain.Assessments;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Simulations.Models;
using Xunit;

namespace SimuLearn.Tests;

/// <summary>
/// The simulation models are the trusted calculation layer: these tests pin the physics the tutor
/// is allowed to quote and the validation rules that keep provider output inside the allowlist.
/// </summary>
public sealed class SimulationModelTests
{
    private readonly DcCircuitModel _circuit = new();
    private readonly InclinedPlaneModel _incline = new();

    [Fact]
    public void Circuit_applies_ohms_law_and_power_relation()
    {
        var outcome = _circuit.Simulate(new ParameterSet(new Dictionary<string, double>
        {
            ["voltageV"] = 12,
            ["resistanceOhm"] = 6,
        }));

        Assert.Equal(2d, outcome.Find("current")!.Value, 6);
        Assert.Equal(24d, outcome.Find("power")!.Value, 6);
        Assert.False(_circuit.Simulate(_circuit.CreateInitialState()).IsInMotion);
    }

    [Fact]
    public void Circuit_current_is_inversely_proportional_to_resistance()
    {
        var baseline = new ParameterSet(new Dictionary<string, double> { ["voltageV"] = 12, ["resistanceOhm"] = 6 });
        var doubled = baseline.With("resistanceOhm", 12);

        var baselineCurrent = _circuit.Simulate(baseline).Find("current")!.Value;
        var doubledCurrent = _circuit.Simulate(doubled).Find("current")!.Value;

        Assert.Equal(baselineCurrent / 2d, doubledCurrent, 6);
    }

    [Fact]
    public void Circuit_explains_a_doubling_of_resistance()
    {
        var before = new ParameterSet(new Dictionary<string, double> { ["voltageV"] = 12, ["resistanceOhm"] = 6 });
        var after = before.With("resistanceOhm", 12);

        var explanation = _circuit.ExplainChange(
            before,
            _circuit.Simulate(before),
            after,
            _circuit.Simulate(after));

        Assert.Contains("inversely proportional", explanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("halves", explanation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validation_rejects_out_of_range_and_unknown_parameters()
    {
        var result = _circuit.Validate(new ParameterSet(new Dictionary<string, double>
        {
            ["voltageV"] = 12,
            ["resistanceOhm"] = 500,
            ["massKg"] = 2,
        }));

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == "out_of_range");
        Assert.Contains(result.Issues, issue => issue.Code == "unknown_parameter");
    }

    [Fact]
    public void Normalize_fills_defaults_clamps_and_drops_unknown_keys()
    {
        var normalized = _circuit.Normalize(new ParameterSet(new Dictionary<string, double>
        {
            ["resistanceOhm"] = 1000,
            ["bogus"] = 3,
        }));

        Assert.Equal(12d, normalized["voltageV"]);
        Assert.Equal(100d, normalized["resistanceOhm"]);
        Assert.False(normalized.TryGet("bogus", out _));
    }

    [Fact]
    public void Simulate_throws_for_an_invalid_state()
    {
        var invalid = new ParameterSet(new Dictionary<string, double>
        {
            ["voltageV"] = -5,
            ["resistanceOhm"] = 6,
        });

        Assert.Throws<DomainValidationException>(() => _circuit.Simulate(invalid));
    }

    [Fact]
    public void Incline_follows_the_published_equation()
    {
        var state = new ParameterSet(new Dictionary<string, double>
        {
            ["massKg"] = 2,
            ["angleDegrees"] = 20,
            ["frictionCoefficient"] = 0.15,
            ["rampLengthM"] = 4,
        });

        var radians = 20 * Math.PI / 180d;
        var expected = InclinedPlaneModel.GravitationalAcceleration
                       * (Math.Sin(radians) - 0.15 * Math.Cos(radians));

        var outcome = _incline.Simulate(state);

        Assert.Equal(expected, outcome.Find("acceleration")!.Value, 6);
        Assert.True(outcome.IsInMotion);
        Assert.NotEmpty(outcome.Trajectory);
        Assert.True(outcome.Trajectory[^1].VelocityMetresPerSecond > 0);
    }

    [Fact]
    public void Incline_stays_at_rest_when_friction_holds_the_block()
    {
        var state = new ParameterSet(new Dictionary<string, double>
        {
            ["massKg"] = 2,
            ["angleDegrees"] = 10,
            ["frictionCoefficient"] = 0.8,
            ["rampLengthM"] = 4,
        });

        var outcome = _incline.Simulate(state);

        Assert.Equal(0d, outcome.Find("acceleration")!.Value, 9);
        Assert.False(outcome.IsInMotion);
        Assert.Single(outcome.Trajectory);
        Assert.Equal(0d, outcome.Trajectory[0].PositionMetres, 9);
        Assert.NotEmpty(outcome.Warnings);
    }

    [Fact]
    public void Registry_is_case_insensitive_and_rejects_unknown_models()
    {
        var registry = new SimulationModelRegistry([_circuit, _incline]);

        Assert.True(registry.TryGet("SIMPLE-DC-CIRCUIT-V1", out var resolved));
        Assert.Same(_circuit, resolved);
        Assert.Throws<NotFoundException>(() => registry.GetRequired("sql-injection"));
    }
}

public sealed class AssessmentScoringTests
{
    private static AssessmentQuestion SingleChoice() => new()
    {
        Id = "q1",
        Prompt = "What happens to the current when resistance doubles at constant voltage?",
        Type = QuestionType.SingleChoice,
        Options = [new("a", "It doubles"), new("b", "It halves")],
        CorrectOptionIds = ["b"],
        Explanation = "Current is inversely proportional to resistance.",
    };

    private static AssessmentQuestion MultiSelect() => new()
    {
        Id = "q2",
        Prompt = "Which quantities are needed to compute power dissipated by a resistor?",
        Type = QuestionType.MultipleChoice,
        Options = [new("a", "Voltage"), new("b", "Mass"), new("c", "Current")],
        CorrectOptionIds = ["a", "c"],
        Explanation = "P = V × I.",
    };

    private static AssessmentQuestion Numeric() => new()
    {
        Id = "q3",
        Prompt = "A 12 V source drives a 6 ohm resistor. What is the current in amperes?",
        Type = QuestionType.Numeric,
        NumericAnswer = 2,
        Explanation = "I = V / R = 12 / 6.",
    };

    [Fact]
    public void Correct_answers_score_full_marks()
    {
        var result = AssessmentScorer.Score(
            [SingleChoice(), Numeric()],
            [
                new AssessmentAnswer("q1", ["b"]),
                new AssessmentAnswer("q3", NumericValue: 2.01),
            ]);

        Assert.Equal(100d, result.Percentage);
        Assert.True(result.Passed);
        Assert.All(result.Feedback, feedback => Assert.True(feedback.Correct));
    }

    [Fact]
    public void Wrong_and_missing_answers_are_marked_incorrect()
    {
        var result = AssessmentScorer.Score(
            [SingleChoice(), Numeric()],
            [new AssessmentAnswer("q1", ["a"])]);

        Assert.Equal(0d, result.Percentage);
        Assert.False(result.Passed);
        Assert.Null(result.Feedback[1].SubmittedAnswer);
    }

    [Fact]
    public void Multi_select_requires_an_exact_match()
    {
        var partial = AssessmentScorer.Score([MultiSelect()], [new AssessmentAnswer("q2", ["a"])]);
        var exact = AssessmentScorer.Score([MultiSelect()], [new AssessmentAnswer("q2", ["c", "a"])]);

        Assert.Equal(0d, partial.Percentage);
        Assert.Equal(100d, exact.Percentage);
    }

    [Fact]
    public void Numeric_answers_outside_tolerance_fail()
    {
        var within = AssessmentScorer.Score([Numeric()], [new AssessmentAnswer("q3", NumericValue: 2.01)]);
        var outside = AssessmentScorer.Score([Numeric()], [new AssessmentAnswer("q3", NumericValue: 2.5)]);

        Assert.True(within.Passed);
        Assert.False(outside.Passed);
    }
}

public sealed class SceneSpecificationValidatorTests
{
    private readonly SceneSpecificationValidator _validator = new(
        new SimulationModelRegistry([new DcCircuitModel(), new InclinedPlaneModel()]));

    private static string Scene(string model = "simple-dc-circuit-v1", string initialState = """{"voltageV":12,"resistanceOhm":6}""") =>
        $$"""
        {
          "simulationModel": "{{model}}",
          "initialState": {{initialState}},
          "controls": [{ "parameter": "voltageV" }]
        }
        """;

    [Fact]
    public void Valid_scene_is_canonicalised_by_the_server()
    {
        var result = _validator.ValidateJson(Scene(), source: "generated", provider: "test");

        Assert.True(result.IsValid, string.Join("; ", result.Errors));
        Assert.Equal("simple-dc-circuit-v1", result.Specification!.SimulationModel);
        Assert.Equal(2, result.Specification.Controls.Count);
        Assert.Equal("generated", result.Specification.Source);
        Assert.Equal("test", result.Specification.Provider);
    }

    [Fact]
    public void Unknown_model_keys_are_rejected()
    {
        var result = _validator.ValidateJson(Scene(model: "custom-html-renderer"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("not supported", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Out_of_range_initial_state_is_rejected()
    {
        var result = _validator.ValidateJson(Scene(initialState: """{"voltageV":9999,"resistanceOhm":6}"""));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.StartsWith("initialState.voltageV", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_json_members_are_rejected()
    {
        var json = """
        {
          "simulationModel": "simple-dc-circuit-v1",
          "initialState": { "voltageV": 12 },
          "controls": [],
          "script": "alert(1)"
        }
        """;

        var result = _validator.ValidateJson(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("schema", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Duplicate_controls_are_rejected()
    {
        var json = """
        {
          "simulationModel": "simple-dc-circuit-v1",
          "initialState": { "voltageV": 12 },
          "controls": [{ "parameter": "voltageV" }, { "parameter": "voltageV" }]
        }
        """;

        var result = _validator.ValidateJson(json);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("more than once", StringComparison.OrdinalIgnoreCase));
    }
}
