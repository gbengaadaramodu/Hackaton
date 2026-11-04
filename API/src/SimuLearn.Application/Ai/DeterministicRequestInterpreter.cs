using System.Text.RegularExpressions;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Simulation;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Ai;

/// <summary>
/// Deterministic, always-available interpreter. It scores the approved simulation catalogue by
/// keyword overlap, then reuses the validated command parser to seed parameters mentioned in the
/// prompt. This is the guaranteed fallback when no language model is configured (FR-016, BR-012).
/// </summary>
public sealed class DeterministicRequestInterpreter(SimulationModelRegistry registry) : IRequestInterpreter
{
    public const string ProviderId = "deterministic-interpreter";

    private static readonly string[] UnsupportedSubjects =
        ["chemistry", "biology", "geography", "economics", "literature", "molecular", "reaction"];

    private readonly SimulationCommandParser _commandParser = new();

    public Task<InterpretationResult> InterpretAsync(InterpretationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prompt = request.Prompt?.Trim() ?? string.Empty;
        if (prompt.Length == 0)
        {
            return Task.FromResult(InterpretationResult.Clarification(
                ProviderId,
                ["What would you like to explore? For example: \"show how ramp angle changes sliding motion\"."],
                "The request was empty.",
                usedFallback: true));
        }

        var ranked = registry.All
            .Select(model => new { Model = model, Score = Score(prompt, model) })
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Model.ModelKey, StringComparer.Ordinal)
            .ToList();

        var best = ranked[0];

        if (best.Score == 0)
        {
            var unsupported = UnsupportedSubjects.Any(subject =>
                prompt.Contains(subject, StringComparison.OrdinalIgnoreCase));

            var questions = new List<string>
            {
                "Which phenomenon should the simulation show?",
                $"Supported topics right now: {string.Join("; ", registry.All.Select(m => m.Title.ToLowerInvariant()))}.",
            };

            return Task.FromResult(InterpretationResult.Clarification(
                ProviderId,
                questions,
                unsupported
                    ? "That subject is not available in the pilot; only validated physics models are enabled."
                    : "The request did not match any approved simulation model.",
                usedFallback: true,
                status: unsupported ? InterpretationStatus.Unsupported : InterpretationStatus.NeedsClarification));
        }

        var runnerUp = ranked.Count > 1 ? ranked[1] : null;
        if (best.Score == 1 && runnerUp is { Score: 1 })
        {
            return Task.FromResult(InterpretationResult.Clarification(
                ProviderId,
                [
                    $"Did you mean \"{best.Model.Title}\" or \"{runnerUp.Model.Title}\"?",
                    "Adding a quantity such as angle, voltage or resistance will help.",
                ],
                "Two approved models matched the request equally well.",
                usedFallback: true));
        }

        var state = best.Model.CreateInitialState();
        var command = _commandParser.Parse(prompt, best.Model, state);
        if (command is not null && command.Type == "set-parameter")
        {
            state = state.With(command.Parameter, command.Value);
        }

        var confidence = Math.Round(Math.Clamp(best.Score / 6d, 0.35d, 0.95d), 2);

        return Task.FromResult(new InterpretationResult
        {
            Status = InterpretationStatus.Ready,
            ModelKey = best.Model.ModelKey,
            Subject = best.Model.Subject,
            Topic = best.Model.Title,
            LearningObjective = best.Model.LearningObjective,
            State = state,
            Confidence = confidence,
            Provider = ProviderId,
            UsedFallback = true,
            Rationale = command is null
                ? $"Matched the approved model by subject keywords ({best.Score} keyword match(es))."
                : $"Matched the approved model by keywords and applied the requested {command.Label.ToLowerInvariant()}.",
        });
    }

    private static int Score(string prompt, ISimulationModel model)
    {
        var score = 0;
        foreach (var keyword in model.Keywords)
        {
            if (keyword.Length <= 2)
            {
                if (Regex.IsMatch(prompt, $@"(?<![a-z]){Regex.Escape(keyword)}(?![a-z])", RegexOptions.IgnoreCase))
                {
                    score++;
                }
            }
            else if (prompt.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                score++;
            }
        }

        if (prompt.Contains(model.Domain, StringComparison.OrdinalIgnoreCase))
        {
            score += 2;
        }

        if (prompt.Contains(model.LearningObjectiveCode, StringComparison.OrdinalIgnoreCase))
        {
            score += 3;
        }

        return score;
    }
}
