using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Requests;

public enum SimulationRequestStatus
{
    Processing = 0,
    Completed = 1,
    NeedsClarification = 2,
    Failed = 3,
}

/// <summary>
/// Durable record of one natural-language request, its interpretation and the validated scene
/// specification. Keeping this row means a failed generation never loses the learner's prompt
/// (SRS 6.4, AC-005).
/// </summary>
public sealed class SimulationRequest : Entity
{
    public Guid UserId { get; set; }

    public required string Prompt { get; set; }

    public string? Subject { get; set; }

    public string? Level { get; set; }

    public SimulationRequestStatus Status { get; set; } = SimulationRequestStatus.Processing;

    /// <summary>Serialized interpretation returned by the interpreter (model or fallback).</summary>
    public string? InterpretationJson { get; set; }

    /// <summary>Serialized, schema-validated <c>SceneSpecification</c> when the request succeeded.</summary>
    public string? SceneJson { get; set; }

    public string? ClarifyingQuestionsJson { get; set; }

    public bool UsedFallback { get; set; }

    public string? Provider { get; set; }

    public string? ErrorCode { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
