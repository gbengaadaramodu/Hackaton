using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Tutoring;

namespace SimuLearn.Application.Ai;

/// <summary>
/// Template-based tutor used whenever no model is configured or a generated answer fails the
/// grounding check. Every sentence is derived from the authoritative
/// <see cref="SimulationOutcome"/> and the model's own published concepts, so it cannot invent a
/// measurement (HAC-006, FR-025).
/// </summary>
public sealed class DeterministicTutorNarrator : ITutorNarrator
{
    public const string ProviderId = "deterministic-tutor";

    public Task<TutorReply> AnswerAsync(
        string question,
        ExplanationLevel level,
        TutorContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var text = BuildAnswer(question ?? string.Empty, level, context);
        return Task.FromResult(new TutorReply(text, ProviderId, true, context.Outcome.Observations));
    }

    private static string BuildAnswer(string question, ExplanationLevel level, TutorContext context)
    {
        var model = context.Model;
        var outcome = context.Outcome;
        var lower = question.ToLowerInvariant();
        var parts = new List<string>();

        var asksAboutChange = ContainsAny(lower, "why", "difference", "compare", "changed", "change", "happen", "effect");
        var baselineAvailable = context.BaselineState is not null && context.BaselineOutcome is not null;

        string opening;
        if (asksAboutChange && baselineAvailable)
        {
            opening = model.ExplainChange(context.BaselineState!, context.BaselineOutcome!, context.State, outcome);
        }
        else if (level == ExplanationLevel.Simpler)
        {
            opening = "In short: " + outcome.Summary;
        }
        else
        {
            opening = outcome.Summary;
        }

        parts.Add(opening);

        var concept = model.Concepts.FirstOrDefault(c =>
            lower.Contains(c.Term.ToLowerInvariant(), StringComparison.Ordinal));

        if (concept is not null && level != ExplanationLevel.Simpler)
        {
            parts.Add($"{concept.Term}: {concept.Definition}");
        }

        if (outcome.Warnings.Count > 0)
        {
            parts.Add(string.Join(" ", outcome.Warnings));
        }

        if (level == ExplanationLevel.Detailed)
        {
            parts.Add("Measured and calculated values: " + string.Join(
                "; ",
                outcome.Observations.Select(o => $"{o.Label} = {o.Display}")) + ".");

            parts.Add("Assumptions: " + string.Join(" ", model.Assumptions));
            parts.Add("Limitations: " + string.Join(" ", model.Limitations));
        }
        else
        {
            var highlights = outcome.Observations.Take(2).Select(o => $"{o.Label} is {o.Display}").ToArray();
            if (highlights.Length > 0)
            {
                parts.Add("From the current run: " + string.Join("; ", highlights) + ".");
            }

            if (level == ExplanationLevel.Standard)
            {
                parts.Add($"This uses the validated relationship {model.GoverningRelationship} and is a simplified " +
                          "classroom model, not a measurement of a real apparatus.");
            }
        }

        parts.Add("These values come from the approved simulation model, not from a language model's estimate.");

        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));
}
