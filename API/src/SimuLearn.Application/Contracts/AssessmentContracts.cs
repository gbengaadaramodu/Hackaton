using SimuLearn.Domain.Assessments;

namespace SimuLearn.Application.Contracts;

public sealed record AssessmentSummaryDto(
    Guid Id,
    string Title,
    string? ModelKey,
    Guid? LessonId,
    int Version,
    int QuestionCount);

public sealed record AssessmentQuestionDto(
    string Id,
    string Prompt,
    QuestionType Type,
    IReadOnlyList<AssessmentOption> Options,
    string? LearningObjectiveCode);

public sealed record AssessmentDto(
    Guid Id,
    string Title,
    string? ModelKey,
    Guid? LessonId,
    int Version,
    IReadOnlyList<AssessmentQuestionDto> Questions);

public sealed record SubmitAttemptRequest(
    IReadOnlyList<AssessmentAnswer> Answers,
    Guid? SessionId = null);

public sealed record AttemptResultDto(
    Guid AttemptId,
    Guid AssessmentId,
    int AssessmentVersion,
    double Score,
    double MaxScore,
    double Percentage,
    bool Passed,
    IReadOnlyList<QuestionFeedback> Feedback,
    DateTimeOffset SubmittedAt);

public sealed record AttemptSummaryDto(
    Guid AttemptId,
    Guid AssessmentId,
    string AssessmentTitle,
    int AssessmentVersion,
    double Percentage,
    bool Passed,
    DateTimeOffset SubmittedAt);
