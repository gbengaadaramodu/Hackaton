using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Common;
using SimuLearn.Domain.Analytics;

namespace SimuLearn.Infrastructure.Analytics;

/// <summary>
/// Adds structured learning events and audit entries to the current unit of work so they commit
/// atomically with the operation that produced them.
/// </summary>
public sealed class LearningEventRecorder(IApplicationDbContext db) : ILearningEventRecorder
{
    public void Record(
        Guid userId,
        LearningEventType type,
        Guid? sessionId = null,
        Guid? assessmentAttemptId = null,
        object? payload = null)
    {
        db.LearningEvents.Add(new LearningEvent
        {
            UserId = userId,
            Type = type,
            SessionId = sessionId,
            AssessmentAttemptId = assessmentAttemptId,
            PayloadJson = JsonDefaults.Serialize(payload ?? new { }),
        });
    }

    public void Audit(
        Actor? actor,
        string action,
        string targetType,
        Guid? targetId = null,
        object? metadata = null,
        string? correlationId = null)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            ActorId = actor?.Id,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            MetadataJson = JsonDefaults.Serialize(metadata ?? new { }),
            CorrelationId = correlationId,
        });
    }
}
