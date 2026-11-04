using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Common;
using SimuLearn.Application.Contracts;
using SimuLearn.Domain.Analytics;
using SimuLearn.Domain.Assessments;
using SimuLearn.Domain.Common;

namespace SimuLearn.Application.Services;

/// <summary>Assessment retrieval and deterministic, server-side scoring (FR-061, FR-066).</summary>
public sealed class AssessmentService(IApplicationDbContext db, ILearningEventRecorder events)
{
    private const int MaxAnswers = 100;

    public async Task<IReadOnlyList<AssessmentSummaryDto>> ListForLessonAsync(
        Actor actor,
        Guid lessonId,
        CancellationToken cancellationToken)
    {
        var query = db.Assessments.AsNoTracking().Where(a => a.LessonId == lessonId);
        if (actor.IsStudent)
        {
            query = query.Where(a => a.Status == AssessmentStatus.Published);
        }

        var assessments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return assessments
            .Select(a => new AssessmentSummaryDto(
                a.Id,
                a.Title,
                a.ModelKey,
                a.LessonId,
                a.Version,
                ReadQuestions(a).Count))
            .ToList();
    }

    public async Task<IReadOnlyList<AssessmentSummaryDto>> ListPublishedAsync(
        Guid? lessonId,
        CancellationToken cancellationToken)
    {
        var query = db.Assessments
            .AsNoTracking()
            .Where(a => a.Status == AssessmentStatus.Published);

        if (lessonId is { } filter)
        {
            query = query.Where(a => a.LessonId == filter);
        }

        var assessments = await query.OrderByDescending(a => a.CreatedAt).ToListAsync(cancellationToken);
        return assessments
            .Select(a => new AssessmentSummaryDto(
                a.Id,
                a.Title,
                a.ModelKey,
                a.LessonId,
                a.Version,
                ReadQuestions(a).Count))
            .ToList();
    }

    public async Task<AssessmentDto> GetAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var assessment = await db.Assessments
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assessment", id);

        if (assessment.Status != AssessmentStatus.Published && actor.IsStudent)
        {
            throw new NotFoundException("Assessment", id);
        }

        var questions = ReadQuestions(assessment);

        return new AssessmentDto(
            assessment.Id,
            assessment.Title,
            assessment.ModelKey,
            assessment.LessonId,
            assessment.Version,
            questions.Select(q => new AssessmentQuestionDto(
                q.Id,
                q.Prompt,
                q.Type,
                q.Options,
                q.LearningObjectiveCode)).ToList());
    }

    public async Task<AttemptResultDto> SubmitAsync(
        Actor actor,
        Guid id,
        SubmitAttemptRequest request,
        CancellationToken cancellationToken)
    {
        var assessment = await db.Assessments
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken)
            ?? throw new NotFoundException("Assessment", id);

        if (assessment.Status != AssessmentStatus.Published && !actor.IsTeacher && !actor.IsAdministrator)
        {
            throw new ConflictException("This assessment is not open for submissions.", "assessment_closed");
        }

        var answers = request.Answers ?? [];
        if (answers.Count > MaxAnswers)
        {
            throw Validation("answers", $"At most {MaxAnswers} answers may be submitted.");
        }

        var questions = ReadQuestions(assessment);
        var known = questions.Select(q => q.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unknown = answers
            .Where(a => !known.Contains(a.QuestionId?.Trim() ?? string.Empty))
            .Select(a => a.QuestionId)
            .Where(q => !string.IsNullOrWhiteSpace(q))
            .ToList();

        if (unknown.Count > 0)
        {
            throw Validation("answers", $"Unknown question id(s): {string.Join(", ", unknown)}.");
        }

        if (answers.Any(a => a.SelectedOptionIds is { Count: > 10 }))
        {
            throw Validation("answers", "Too many selected options for one question.");
        }

        var result = AssessmentScorer.Score(questions, answers);

        if (request.SessionId is { } sessionId)
        {
            var ownsSession = await db.Sessions.AnyAsync(
                s => s.Id == sessionId && s.UserId == actor.Id,
                cancellationToken);

            if (!ownsSession)
            {
                throw new ForbiddenException("That session does not belong to your account.");
            }
        }

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessment.Id,
            AssessmentVersion = assessment.Version,
            UserId = actor.Id,
            SessionId = request.SessionId,
            Score = result.Score,
            MaxScore = result.MaxScore,
            Percentage = result.Percentage,
            Passed = result.Passed,
            AnswersJson = JsonDefaults.Serialize(answers),
            FeedbackJson = JsonDefaults.Serialize(result.Feedback),
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        db.AssessmentAttempts.Add(attempt);
        await db.SaveChangesAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.AssessmentSubmitted, request.SessionId, attempt.Id, new
        {
            assessmentId = assessment.Id,
            assessmentVersion = assessment.Version,
            percentage = result.Percentage,
            passed = result.Passed,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AttemptResultDto(
            attempt.Id,
            assessment.Id,
            assessment.Version,
            result.Score,
            result.MaxScore,
            result.Percentage,
            result.Passed,
            result.Feedback,
            attempt.SubmittedAt);
    }

    public async Task<PagedResult<AttemptSummaryDto>> ListAttemptsAsync(
        Actor actor,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var query = db.AssessmentAttempts
            .AsNoTracking()
            .Where(a => a.UserId == actor.Id);

        var total = await query.CountAsync(cancellationToken);

        var attempts = await query
            .OrderByDescending(a => a.SubmittedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(a => new AttemptSummaryDto(
                a.Id,
                a.AssessmentId,
                a.Assessment != null ? a.Assessment.Title : string.Empty,
                a.AssessmentVersion,
                a.Percentage,
                a.Passed,
                a.SubmittedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<AttemptSummaryDto>(attempts, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    public static List<AssessmentQuestion> ReadQuestions(Assessment assessment)
    {
        var questions = JsonDefaults.Deserialize<List<AssessmentQuestion>>(assessment.QuestionsJson) ?? [];
        return questions;
    }

    private static DomainValidationException Validation(string field, string message) =>
        new("The submission is not valid.", new Dictionary<string, string[]> { [field] = [message] });
}
