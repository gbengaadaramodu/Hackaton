namespace SimuLearn.Application.Contracts;

public sealed record CreateClassRequest(string Name, Guid? InstitutionId = null);

public sealed record JoinClassRequest(string JoinCode);

public sealed record ClassDto(
    Guid Id,
    string Name,
    string JoinCode,
    Guid TeacherId,
    string TeacherName,
    int MemberCount,
    Guid? InstitutionId,
    DateTimeOffset CreatedAt);

public sealed record ClassMemberDto(Guid StudentId, string DisplayName, string Email, DateTimeOffset JoinedAt);

public sealed record CreateAssignmentRequest(
    Guid ClassId,
    Guid LessonId,
    DateTimeOffset? DueAt = null,
    string? Instructions = null);

public sealed record AssignmentDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid LessonId,
    string LessonTitle,
    string ModelKey,
    string? Instructions,
    DateTimeOffset? DueAt,
    Guid TeacherId,
    DateTimeOffset CreatedAt,
    bool Completed = false,
    Guid? LastSessionId = null,
    double? BestAssessmentPercentage = null);
