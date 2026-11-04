using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Sessions;

public enum SessionStatus
{
    Active = 0,
    Paused = 1,
    Completed = 2,
    Abandoned = 3,
}

public enum SnapshotReason
{
    Created = 0,
    Command = 1,
    Reset = 2,
    Completed = 3,
}

public enum CommandType
{
    SetParameter = 0,
}

/// <summary>
/// One learner's interactive run of a simulation model. <see cref="Version"/> is the optimistic
/// concurrency token: a stale command is rejected with a conflict instead of overwriting state.
/// </summary>
public sealed class SimulationSession : Entity
{
    public Guid UserId { get; set; }

    public required string ModelKey { get; set; }

    public Guid TemplateId { get; set; }

    public Guid? LessonId { get; set; }

    public Guid? AssignmentId { get; set; }

    public Guid? SimulationRequestId { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    /// <summary>State the session was created with, used by reset and by comparisons (FR-036, FR-038).</summary>
    public required string InitialStateJson { get; set; }

    /// <summary>Authoritative current state, serialized as a name/value map.</summary>
    public required string StateJson { get; set; }

    public int Version { get; set; } = 1;

    public int CommandCount { get; set; }

    public int MessageCount { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? EndedAt { get; set; }

    public List<SessionSnapshot> Snapshots { get; set; } = [];
}

/// <summary>Immutable snapshot of session state for audit and reproducibility.</summary>
public sealed class SessionSnapshot : Entity
{
    public Guid SessionId { get; set; }

    public SimulationSession? Session { get; set; }

    public required string StateJson { get; set; }

    public SnapshotReason Reason { get; set; }

    public int Version { get; set; }

    public Guid? ActorId { get; set; }
}
