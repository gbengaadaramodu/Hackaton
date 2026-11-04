namespace SimuLearn.Application.Contracts;

public sealed record StudentProgressDto(
    Guid StudentId,
    string DisplayName,
    int Sessions,
    int CompletedSessions,
    int Attempts,
    double? AveragePercentage,
    DateTimeOffset? LastActivityAt);

public sealed record CommonlyMissedQuestionDto(
    Guid AssessmentId,
    string QuestionId,
    string Prompt,
    int Attempts,
    int CorrectAnswers,
    double CorrectRate);

public sealed record ObjectiveMasteryDto(
    string Code,
    string Description,
    int Attempts,
    int CorrectAnswers,
    double? MasteryRate);

public sealed record ClassReportDto(
    Guid ClassId,
    string ClassName,
    int StudentCount,
    int SessionsStarted,
    int SessionsCompleted,
    int AttemptsSubmitted,
    double? AveragePercentage,
    double CompletionRate,
    IReadOnlyList<StudentProgressDto> Students,
    IReadOnlyList<CommonlyMissedQuestionDto> CommonlyMissedQuestions,
    IReadOnlyList<ObjectiveMasteryDto> Objectives,
    DateTimeOffset GeneratedAt);

public sealed record StudentReportDto(
    Guid UserId,
    string DisplayName,
    int SessionsStarted,
    int SessionsCompleted,
    int Attempts,
    double? AveragePercentage,
    IReadOnlyList<AttemptSummaryDto> RecentAttempts,
    IReadOnlyList<ObjectiveMasteryDto> Objectives,
    DateTimeOffset GeneratedAt);
