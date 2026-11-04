using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Assessments;

public enum AssessmentStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2,
}

public sealed class Assessment : Entity
{
    public required string Title { get; set; }

    public Guid? LessonId { get; set; }

    /// <summary>Allowlisted model this assessment belongs to; null means it spans no single model.</summary>
    public string? ModelKey { get; set; }

    public AssessmentStatus Status { get; set; } = AssessmentStatus.Draft;

    /// <summary>Serialized list of <see cref="AssessmentQuestion"/> with answers and explanations.</summary>
    public required string QuestionsJson { get; set; }

    public int Version { get; set; } = 1;

    public DateTimeOffset? PublishedAt { get; set; }
}

/// <summary>
/// A submitted attempt. The assessment version is stored so later edits never rewrite historical
/// results (FR-066).
/// </summary>
public sealed class AssessmentAttempt : Entity
{
    public Guid AssessmentId { get; set; }

    public Assessment? Assessment { get; set; }

    public int AssessmentVersion { get; set; }

    public Guid UserId { get; set; }

    public Guid? SessionId { get; set; }

    public double Score { get; set; }

    public double MaxScore { get; set; }

    public double Percentage { get; set; }

    public bool Passed { get; set; }

    public required string AnswersJson { get; set; }

    public required string FeedbackJson { get; set; }

    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
}
