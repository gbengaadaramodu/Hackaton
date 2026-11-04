using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Tutoring;

public enum MessageRole
{
    Student = 0,
    Tutor = 1,
    System = 2,
}

public enum ExplanationLevel
{
    Simpler = 0,
    Standard = 1,
    Detailed = 2,
}

public sealed class Conversation : Entity
{
    public Guid SessionId { get; set; }

    public Guid UserId { get; set; }

    public List<Message> Messages { get; set; } = [];
}

/// <summary>
/// Retained conversational message. Retention is policy-controlled (BRD 7.3); raw audio is never
/// stored by the API.
/// </summary>
public sealed class Message : Entity
{
    public Guid ConversationId { get; set; }

    public Conversation? Conversation { get; set; }

    public MessageRole Role { get; set; }

    public required string Content { get; set; }

    public bool IsAiGenerated { get; set; }

    public ExplanationLevel Level { get; set; } = ExplanationLevel.Standard;

    /// <summary>Provider identifier, for example "deterministic-tutor" or "meta-llm:&lt;model&gt;".</summary>
    public string? Provider { get; set; }

    /// <summary>Calculated values the answer was allowed to reference (HAC-006 evidence).</summary>
    public string? GroundingJson { get; set; }
}
