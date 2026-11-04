using System.Globalization;
using System.Text.RegularExpressions;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Simulation;

/// <summary>
/// Result of turning a learner sentence into one validated parameter change. Parsing is
/// deterministic and always bounded by the model's own parameter ranges (FR-021, FR-035).
/// </summary>
public sealed record ParsedSimulationCommand(
    string Type,
    string Parameter,
    string Label,
    double PreviousValue,
    double Value,
    string Unit,
    bool WasClamped,
    string Explanation);

/// <summary>
/// Small, auditable natural-language command parser for supported parameter changes such as
/// "set the ramp angle to 35 degrees", "double the voltage" or "reduce friction by 20%".
/// Unsupported phrasing returns <c>null</c> so callers can ask a clarifying question instead of
/// guessing (FR-014, FR-027).
/// </summary>
public sealed partial class SimulationCommandParser
{
    private static readonly (string Term, string[] Parameters)[] Synonyms =
    [
        ("ramp angle", ["angleDegrees"]),
        ("angle", ["angleDegrees"]),
        ("steep", ["angleDegrees"]),
        ("slope", ["angleDegrees"]),
        ("inclination", ["angleDegrees"]),
        ("incline", ["angleDegrees"]),
        ("friction coefficient", ["frictionCoefficient"]),
        ("friction", ["frictionCoefficient"]),
        ("rough", ["frictionCoefficient"]),
        ("smooth", ["frictionCoefficient"]),
        ("coefficient", ["frictionCoefficient"]),
        ("ramp length", ["rampLengthM"]),
        ("length", ["rampLengthM"]),
        ("mass", ["massKg"]),
        ("weight", ["massKg"]),
        ("heavy", ["massKg"]),
        ("voltage", ["voltageV"]),
        ("battery", ["voltageV"]),
        ("volt", ["voltageV"]),
        ("resistance", ["resistanceOhm"]),
        ("resistor", ["resistanceOhm"]),
        ("ohm", ["resistanceOhm"]),
    ];

    private static readonly Dictionary<string, double> Multipliers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["double"] = 2d,
            ["twice"] = 2d,
            ["triple"] = 3d,
            ["thrice"] = 3d,
            ["halve"] = 0.5d,
            ["half"] = 0.5d,
            ["quarter"] = 0.25d,
        };

    [GeneratedRegex(@"(?<![A-Za-z])(?<value>\d+(?:\.\d+)?)\s*(?:%|percent)", RegexOptions.IgnoreCase)]
    private static partial Regex PercentChangePattern();

    [GeneratedRegex(@"(?<value>\d+(?:\.\d+)?)\s*(?:x|times)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MultiplierPattern();

    [GeneratedRegex(@"\b(?:to|at)\s+(?<value>-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase)]
    private static partial Regex TargetValuePattern();

    [GeneratedRegex(@"-?\d+(?:\.\d+)?")]
    private static partial Regex NumberPattern();

    public ParsedSimulationCommand? Parse(string? text, ISimulationModel model, ParameterSet state)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > 500)
        {
            return null;
        }

        var normalized = text.Trim().ToLowerInvariant();
        var parameter = FindParameter(normalized, model);
        if (parameter is null)
        {
            return null;
        }

        var current = state.TryGet(parameter.Name, out var existing) ? existing : parameter.Default;
        var target = ExtractTargetValue(normalized, parameter, current);
        if (target is null)
        {
            return null;
        }

        var (rawValue, basis) = target.Value;
        var rounded = RoundToStep(rawValue, parameter.Step);
        var clamped = Math.Clamp(rounded, parameter.Min, parameter.Max);
        var wasClamped = Math.Abs(clamped - rounded) > 1e-9;

        var explanation = wasClamped
            ? $"Setting {parameter.Label.ToLowerInvariant()} {basis} would leave the supported range, so it is held at " +
              $"{ParameterSet.Format(clamped)} {parameter.Unit} instead."
            : $"Setting {parameter.Label.ToLowerInvariant()} {basis}: " +
              $"{ParameterSet.Format(current)} {parameter.Unit} → {ParameterSet.Format(clamped)} {parameter.Unit}.";

        return new ParsedSimulationCommand(
            "set-parameter",
            parameter.Name,
            parameter.Label,
            current,
            clamped,
            parameter.Unit,
            wasClamped,
            explanation);
    }

    private static (double Value, string Basis)? ExtractTargetValue(
        string text,
        ParameterDefinition parameter,
        double current)
    {
        var targetMatch = TargetValuePattern().Match(text);
        if (targetMatch.Success && TryParse(targetMatch.Groups["value"].Value, out var explicitValue))
        {
            return (explicitValue, "to the requested value");
        }

        var percentMatch = PercentChangePattern().Match(text);
        if (percentMatch.Success && TryParse(percentMatch.Groups["value"].Value, out var percent) && percent <= 1000)
        {
            var decrease = DecreaseWords.Any(w => text.Contains(w, StringComparison.Ordinal));
            var factor = decrease ? 1d - (percent / 100d) : 1d + (percent / 100d);
            if (factor < 0)
            {
                factor = 0;
            }

            return (current * factor, $"{(decrease ? "down" : "up")} by {ParameterSet.Format(percent)}%");
        }

        foreach (var (word, factor) in Multipliers)
        {
            if (ContainsWord(text, word))
            {
                return (current * factor, factor switch
                {
                    2d => "to double the current value",
                    3d => "to triple the current value",
                    0.5d => "to halve the current value",
                    _ => $"to {ParameterSet.Format(factor)}× the current value",
                });
            }
        }

        var multiplierMatch = MultiplierPattern().Match(text);
        if (multiplierMatch.Success && TryParse(multiplierMatch.Groups["value"].Value, out var multiplier))
        {
            return (current * multiplier, $"to {ParameterSet.Format(multiplier)}× the current value");
        }

        var numberMatch = NumberPattern().Match(text);
        if (numberMatch.Success && TryParse(numberMatch.Value, out var absolute))
        {
            return (absolute, "to the requested value");
        }

        return null;
    }

    private static readonly string[] DecreaseWords =
        ["reduce", "reduces", "decrease", "decreases", "lower", "lowers", "drop", "drops", "less", "shrink"];

    private static ParameterDefinition? FindParameter(string text, ISimulationModel model)
    {
        ParameterDefinition? best = null;
        var bestLength = 0;

        foreach (var (term, parameterNames) in Synonyms)
        {
            if (!text.Contains(term, StringComparison.Ordinal) || term.Length <= bestLength)
            {
                continue;
            }

            var candidate = model.Parameters.FirstOrDefault(p =>
                parameterNames.Contains(p.Name, StringComparer.OrdinalIgnoreCase));

            if (candidate is null)
            {
                continue;
            }

            best = candidate;
            bestLength = term.Length;
        }

        foreach (var parameter in model.Parameters)
        {
            var candidates = new[]
            {
                parameter.Name,
                SplitCamelCase(parameter.Name),
                parameter.Label,
                parameter.Symbol ?? string.Empty,
            };

            foreach (var candidate in candidates)
            {
                if (candidate.Length <= bestLength)
                {
                    continue;
                }

                if (candidate.Length <= 2
                        ? ContainsWord(text, candidate)
                        : text.Contains(candidate, StringComparison.Ordinal))
                {
                    best = parameter;
                    bestLength = candidate.Length;
                }
            }
        }

        return best;
    }

    private static string SplitCamelCase(string name) =>
        string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? $" {char.ToLowerInvariant(c)}" : c.ToString()))
            .ToLowerInvariant();

    private static bool ContainsWord(string text, string word) =>
        Regex.IsMatch(text, $@"(?<![a-z]){Regex.Escape(word)}(?![a-z])", RegexOptions.IgnoreCase);

    private static bool TryParse(string value, out double parsed) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed)
        && !double.IsNaN(parsed)
        && !double.IsInfinity(parsed);

    private static double RoundToStep(double value, double step)
    {
        if (step > 0)
        {
            value = Math.Round(value / step, MidpointRounding.AwayFromZero) * step;
        }

        return Math.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
