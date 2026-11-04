using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Analytics;

public enum LearningEventType
{
    SimulationRequested = 0,
    SessionStarted = 1,
    SessionPaused = 2,
    SessionResumed = 3,
    SessionCompleted = 4,
    ParameterChanged = 5,
    TutorQuestionAsked = 6,
    AssessmentSubmitted = 7,
    ComparisonRun = 8,
    SessionReset = 9,
    ContentReported = 10,
}

/// <summary>Structured learning event; payloads never contain raw audio or credentials.</summary>
public sealed class LearningEvent : Entity
{
    public Guid UserId { get; set; }

    public Guid? SessionId { get; set; }

    public Guid? AssessmentAttemptId { get; set; }

    public LearningEventType Type { get; set; }

    public string PayloadJson { get; set; } = "{}";
}

/// <summary>Security and administrative audit trail (SEC-007).</summary>
public sealed class AuditEvent : Entity
{
    public Guid? ActorId { get; set; }

    public required string Action { get; set; }

    public required string TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public string? CorrelationId { get; set; }
}
