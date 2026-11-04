using SimuLearn.Domain.Analytics;

namespace SimuLearn.Application.Abstractions;

/// <summary>
/// Records structured learning events and security audit entries. Events are added to the current
/// unit of work and saved with the operation that produced them (BRD 3.4.10, SEC-007).
/// </summary>
public interface ILearningEventRecorder
{
    void Record(
        Guid userId,
        LearningEventType type,
        Guid? sessionId = null,
        Guid? assessmentAttemptId = null,
        object? payload = null);

    void Audit(
        Actor? actor,
        string action,
        string targetType,
        Guid? targetId = null,
        object? metadata = null,
        string? correlationId = null);
}
