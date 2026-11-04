using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Contracts;

public sealed record CreateLessonRequest(
    string Title,
    string SubjectCode,
    string ModelKey,
    Dictionary<string, double>? InitialState = null,
    IReadOnlyList<string>? ObjectiveCodes = null,
    string GradeLevel = "SS2",
    string? Description = null);

public sealed record UpdateLessonRequest(
    string? Title = null,
    string? Description = null,
    string? GradeLevel = null,
    string? SubjectCode = null,
    Dictionary<string, double>? InitialState = null,
    IReadOnlyList<string>? ObjectiveCodes = null);

public sealed record ReviewLessonRequest(ReviewDecision Decision, string? Comments = null);

public sealed record LessonDto(
    Guid Id,
    string Title,
    string? Description,
    string SubjectCode,
    string SubjectName,
    string GradeLevel,
    string ModelKey,
    LessonStatus Status,
    int DraftVersion,
    int PublishedVersion,
    DateTimeOffset? PublishedAt,
    Guid AuthorId,
    IReadOnlyList<ObjectiveDto> Objectives,
    SceneSpecification DraftScene,
    SceneSpecification? PublishedScene,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PublishedLessonDto(
    Guid Id,
    string Title,
    string? Description,
    string SubjectCode,
    string GradeLevel,
    string ModelKey,
    int PublishedVersion,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<ObjectiveDto> Objectives,
    SceneSpecification Scene);
