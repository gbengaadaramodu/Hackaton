using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Abstractions;

public sealed record InterpretationRequest(string Prompt, string? Subject = null, string? Level = null);

public enum InterpretationStatus
{
    Ready = 0,
    NeedsClarification = 1,
    Unsupported = 2,
}

/// <summary>
/// Result of turning a learner's natural-language request into a validated simulation selection.
/// Only a model key and in-range parameter values cross this boundary; never code or markup.
/// </summary>
public sealed record InterpretationResult
{
    public required InterpretationStatus Status { get; init; }

    public string? ModelKey { get; init; }

    public string? Subject { get; init; }

    public string? Topic { get; init; }

    public string? LearningObjective { get; init; }

    public ParameterSet State { get; init; } = ParameterSet.Empty;

    public double Confidence { get; init; }

    public IReadOnlyList<string> ClarifyingQuestions { get; init; } = [];

    public required string Provider { get; init; }

    public bool UsedFallback { get; init; }

    public string Rationale { get; init; } = string.Empty;

    public static InterpretationResult Clarification(
        string provider,
        IReadOnlyList<string> questions,
        string rationale,
        bool usedFallback = false,
        InterpretationStatus status = InterpretationStatus.NeedsClarification) =>
        new()
        {
            Status = status,
            Provider = provider,
            ClarifyingQuestions = questions,
            Rationale = rationale,
            UsedFallback = usedFallback,
        };
}

public interface IRequestInterpreter
{
    Task<InterpretationResult> InterpretAsync(InterpretationRequest request, CancellationToken cancellationToken);
}
