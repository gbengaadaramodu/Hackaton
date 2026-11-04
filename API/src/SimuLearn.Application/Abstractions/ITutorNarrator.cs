using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Tutoring;

namespace SimuLearn.Application.Abstractions;

public sealed record TutorHistoryEntry(string Role, string Content);

public sealed record TutorContext
{
    public required ISimulationModel Model { get; init; }

    public required ParameterSet State { get; init; }

    public required SimulationOutcome Outcome { get; init; }

    public ParameterSet? BaselineState { get; init; }

    public SimulationOutcome? BaselineOutcome { get; init; }

    public IReadOnlyList<TutorHistoryEntry> History { get; init; } = [];
}

public sealed record TutorReply(
    string Text,
    string Provider,
    bool UsedFallback,
    IReadOnlyList<SimulationObservation> GroundedFacts);

public interface ITutorNarrator
{
    /// <summary>
    /// Answers a learner question using only calculated values from <paramref name="context"/>.
    /// Implementations must never compute physics or invent measurements.
    /// </summary>
    Task<TutorReply> AnswerAsync(
        string question,
        ExplanationLevel level,
        TutorContext context,
        CancellationToken cancellationToken);
}
