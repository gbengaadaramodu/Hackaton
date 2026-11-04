using SimuLearn.Domain.Common;
using SimuLearn.Domain.Curriculum;

namespace SimuLearn.Domain.Lessons;

public enum LessonStatus
{
    Draft = 0,
    InReview = 1,
    Published = 2,
    Archived = 3,
}

/// <summary>
/// A reusable learning experience built on one approved simulation model.
/// Draft and published configurations are stored separately so editing a lesson never silently
/// alters an assessment that is already in progress (BRD 6.2, FR-017).
/// </summary>
public sealed class Lesson : Entity
{
    public required string Title { get; set; }

    public string? Description { get; set; }

    public Guid SubjectId { get; set; }

    public Subject? Subject { get; set; }

    public Guid AuthorId { get; set; }

    public required string GradeLevel { get; set; }

    /// <summary>Allowlisted simulation model key; never free-form executable content.</summary>
    public required string ModelKey { get; set; }

    public LessonStatus Status { get; set; } = LessonStatus.Draft;

    /// <summary>Serialized <c>{ "initialState": { ... } }</c> edited by the teacher.</summary>
    public required string DraftConfigurationJson { get; set; }

    public string? PublishedConfigurationJson { get; set; }

    public int DraftVersion { get; set; } = 1;

    public int PublishedVersion { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public Guid? PublishedByUserId { get; set; }

    public List<LessonObjective> Objectives { get; set; } = [];

    public bool IsPublic => Status == LessonStatus.Published;
}

public sealed class LessonObjective
{
    public Guid LessonId { get; set; }

    public Lesson? Lesson { get; set; }

    public Guid LearningObjectiveId { get; set; }

    public LearningObjective? LearningObjective { get; set; }
}

public enum ReviewDecision
{
    Approved = 0,
    ChangesRequested = 1,
    Rejected = 2,
}

/// <summary>Approval history for content review and publication (BR-002, BR-010).</summary>
public sealed class ContentReview : Entity
{
    public Guid LessonId { get; set; }

    public Lesson? Lesson { get; set; }

    public Guid ReviewerId { get; set; }

    public ReviewDecision Decision { get; set; }

    public int LessonVersion { get; set; }

    public string? Comments { get; set; }
}

public enum ContentReportStatus
{
    Open = 0,
    InReview = 1,
    Resolved = 2,
}

/// <summary>Teacher/student issue reports about lessons, explanations or simulation behaviour.</summary>
public sealed class ContentReport : Entity
{
    public Guid ReportedByUserId { get; set; }

    public required string TargetType { get; set; }

    public Guid? TargetId { get; set; }

    public required string Category { get; set; }

    public required string Description { get; set; }

    public ContentReportStatus Status { get; set; } = ContentReportStatus.Open;

    public string? ResolutionNotes { get; set; }
}
