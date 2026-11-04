using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Curriculum;

public sealed class Subject : Entity
{
    public required string Name { get; set; }

    public required string Code { get; set; }

    public bool IsActive { get; set; } = true;

    public List<LearningObjective> LearningObjectives { get; set; } = [];
}

/// <summary>
/// A configurable curriculum reference (for example the Nigerian senior secondary physics syllabus).
/// The platform never assumes a single national standard (BRD 2.5).
/// </summary>
public sealed class CurriculumStandard : Entity
{
    public required string Region { get; set; }

    public required string CurriculumCode { get; set; }

    public required string Version { get; set; }

    public required string Title { get; set; }

    public List<LearningObjective> LearningObjectives { get; set; } = [];
}

public sealed class LearningObjective : Entity
{
    public required string Code { get; set; }

    public required string Description { get; set; }

    /// <summary>Target learning level, for example "SS2".</summary>
    public required string Level { get; set; }

    public Guid SubjectId { get; set; }

    public Subject? Subject { get; set; }

    public Guid? CurriculumStandardId { get; set; }

    public CurriculumStandard? CurriculumStandard { get; set; }
}
