using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Common;
using SimuLearn.Application.Contracts;
using SimuLearn.Domain.Assessments;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Sessions;

namespace SimuLearn.Application.Services;

/// <summary>
/// Teacher and student reporting. Results are derived from stored sessions and deterministic
/// assessment feedback; the AI is never asked to infer a grade (FR-064, BRD 10.2).
/// </summary>
public sealed class ReportService(IApplicationDbContext db)
{
    public async Task<ClassReportDto> GetClassReportAsync(
        Actor actor,
        Guid classId,
        CancellationToken cancellationToken)
    {
        var classRoom = await db.ClassRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == classId, cancellationToken)
            ?? throw new NotFoundException("Class", classId);

        if (classRoom.TeacherId != actor.Id && !actor.IsAdministratorOrReviewer)
        {
            throw new ForbiddenException("You can only report on your own classes.");
        }

        var memberIds = await db.ClassMembers
            .AsNoTracking()
            .Where(m => m.ClassRoomId == classId)
            .Select(m => m.StudentId)
            .ToListAsync(cancellationToken);

        var students = await db.Users
            .AsNoTracking()
            .Where(u => memberIds.Contains(u.Id))
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(cancellationToken);

        var sessions = await db.Sessions
            .AsNoTracking()
            .Where(s => memberIds.Contains(s.UserId))
            .Select(s => new { s.UserId, s.Status, s.StartedAt, s.LastActivityAt })
            .ToListAsync(cancellationToken);

        var attempts = await db.AssessmentAttempts
            .AsNoTracking()
            .Where(a => memberIds.Contains(a.UserId))
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.AssessmentId,
                a.AssessmentVersion,
                a.Percentage,
                a.Passed,
                a.SubmittedAt,
                a.FeedbackJson,
            })
            .ToListAsync(cancellationToken);

        var progress = students
            .Select(student =>
            {
                var studentSessions = sessions.Where(s => s.UserId == student.Id).ToList();
                var studentAttempts = attempts.Where(a => a.UserId == student.Id).ToList();

                return new StudentProgressDto(
                    student.Id,
                    student.DisplayName,
                    studentSessions.Count,
                    studentSessions.Count(s => s.Status == SessionStatus.Completed),
                    studentAttempts.Count,
                    studentAttempts.Count == 0
                        ? null
                        : Math.Round(studentAttempts.Average(a => a.Percentage), 1),
                    studentSessions.Count == 0
                        ? null
                        : studentSessions.Max(s => s.LastActivityAt));
            })
            .OrderBy(p => p.DisplayName)
            .ToList();

        var questionsByAssessment = await LoadQuestionsAsync(
            attempts.Select(a => a.AssessmentId).Distinct().ToList(),
            cancellationToken);

        var feedback = attempts
            .Select(a => new AttemptFeedback(
                a.AssessmentId,
                JsonDefaults.Deserialize<List<QuestionFeedback>>(a.FeedbackJson) ?? []))
            .Where(a => a.Feedback.Count > 0)
            .ToList();

        var missed = BuildMissedQuestions(feedback, questionsByAssessment);
        var objectives = BuildObjectiveMastery(feedback, questionsByAssessment);
        await FillObjectiveDescriptionsAsync(objectives, cancellationToken);

        var completedSessions = sessions.Count(s => s.Status == SessionStatus.Completed);

        return new ClassReportDto(
            classRoom.Id,
            classRoom.Name,
            memberIds.Count,
            sessions.Count,
            completedSessions,
            attempts.Count,
            attempts.Count == 0 ? null : Math.Round(attempts.Average(a => a.Percentage), 1),
            sessions.Count == 0 ? 0d : Math.Round(completedSessions / (double)sessions.Count * 100d, 1),
            progress,
            missed,
            objectives,
            DateTimeOffset.UtcNow);
    }

    public async Task<StudentReportDto> GetStudentReportAsync(
        Actor actor,
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (studentId != actor.Id && !actor.IsAdministratorOrReviewer)
        {
            var teachesStudent = await db.ClassMembers
                .AnyAsync(
                    m => m.StudentId == studentId
                         && m.ClassRoom != null
                         && m.ClassRoom.TeacherId == actor.Id,
                    cancellationToken);

            if (!teachesStudent)
            {
                throw new ForbiddenException("You can only view your own reports or those of your students.");
            }
        }

        var student = await db.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == studentId, cancellationToken)
            ?? throw new NotFoundException("Student", studentId);

        var sessions = await db.Sessions
            .AsNoTracking()
            .Where(s => s.UserId == studentId)
            .Select(s => new { s.Status })
            .ToListAsync(cancellationToken);

        var attempts = await db.AssessmentAttempts
            .AsNoTracking()
            .Where(a => a.UserId == studentId)
            .ToListAsync(cancellationToken);

        var assessmentIds = attempts.Select(a => a.AssessmentId).Distinct().ToList();
        var questionsByAssessment = await LoadQuestionsAsync(assessmentIds, cancellationToken);

        var feedback = attempts
            .Select(a => new AttemptFeedback(
                a.AssessmentId,
                JsonDefaults.Deserialize<List<QuestionFeedback>>(a.FeedbackJson) ?? []))
            .ToList();

        var objectives = BuildObjectiveMastery(feedback, questionsByAssessment);
        await FillObjectiveDescriptionsAsync(objectives, cancellationToken);

        var recent = attempts
            .OrderByDescending(a => a.SubmittedAt)
            .Take(10)
            .Select(a => new AttemptSummaryDto(
                a.Id,
                a.AssessmentId,
                string.Empty,
                a.AssessmentVersion,
                a.Percentage,
                a.Passed,
                a.SubmittedAt))
            .ToList();

        if (recent.Count > 0)
        {
            var titles = await db.Assessments
                .AsNoTracking()
                .Where(a => assessmentIds.Contains(a.Id))
                .Select(a => new { a.Id, a.Title })
                .ToDictionaryAsync(a => a.Id, a => a.Title, cancellationToken);

            recent = recent
                .Select(a => a with { AssessmentTitle = titles.GetValueOrDefault(a.AssessmentId, string.Empty) })
                .ToList();
        }

        return new StudentReportDto(
            student.Id,
            student.DisplayName,
            sessions.Count,
            sessions.Count(s => s.Status == SessionStatus.Completed),
            attempts.Count,
            attempts.Count == 0 ? null : Math.Round(attempts.Average(a => a.Percentage), 1),
            recent,
            objectives,
            DateTimeOffset.UtcNow);
    }

    private async Task<Dictionary<Guid, List<AssessmentQuestion>>> LoadQuestionsAsync(
        List<Guid> assessmentIds,
        CancellationToken cancellationToken)
    {
        if (assessmentIds.Count == 0)
        {
            return [];
        }

        var assessments = await db.Assessments
            .AsNoTracking()
            .Where(a => assessmentIds.Contains(a.Id))
            .ToListAsync(cancellationToken);

        return assessments.ToDictionary(a => a.Id, AssessmentService.ReadQuestions);
    }

    private static List<CommonlyMissedQuestionDto> BuildMissedQuestions(
        IReadOnlyList<AttemptFeedback> feedback,
        IReadOnlyDictionary<Guid, List<AssessmentQuestion>> questionsByAssessment)
    {
        var results = new List<CommonlyMissedQuestionDto>();

        foreach (var (assessmentId, questions) in questionsByAssessment)
        {
            foreach (var question in questions)
            {
                var answers = feedback
                    .Where(f => f.AssessmentId == assessmentId)
                    .SelectMany(f => f.Feedback)
                    .Where(i => i.QuestionId == question.Id)
                    .ToList();

                if (answers.Count == 0)
                {
                    continue;
                }

                var correct = answers.Count(a => a.Correct);
                results.Add(new CommonlyMissedQuestionDto(
                    assessmentId,
                    question.Id,
                    question.Prompt,
                    answers.Count,
                    correct,
                    Math.Round(correct / (double)answers.Count * 100d, 1)));
            }
        }

        return results
            .OrderBy(r => r.CorrectRate)
            .ThenByDescending(r => r.Attempts)
            .Take(10)
            .ToList();
    }

    private static List<ObjectiveMasteryDto> BuildObjectiveMastery(
        IReadOnlyList<AttemptFeedback> feedback,
        IReadOnlyDictionary<Guid, List<AssessmentQuestion>> questionsByAssessment)
    {
        var mastery = new Dictionary<string, (int Attempts, int Correct)>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assessmentId, questions) in questionsByAssessment)
        {
            foreach (var question in questions)
            {
                if (string.IsNullOrWhiteSpace(question.LearningObjectiveCode))
                {
                    continue;
                }

                var items = feedback
                    .Where(f => f.AssessmentId == assessmentId)
                    .SelectMany(f => f.Feedback)
                    .Where(i => i.QuestionId == question.Id);

                foreach (var item in items)
                {
                    var current = mastery.GetValueOrDefault(question.LearningObjectiveCode);
                    mastery[question.LearningObjectiveCode!] = (
                        current.Attempts + 1,
                        current.Correct + (item.Correct ? 1 : 0));
                }
            }
        }

        return mastery
            .Select(pair => new ObjectiveMasteryDto(
                pair.Key,
                pair.Key,
                pair.Value.Attempts,
                pair.Value.Correct,
                Math.Round(pair.Value.Correct / (double)pair.Value.Attempts * 100d, 1)))
            .OrderBy(o => o.MasteryRate)
            .ToList();
    }

    private async Task FillObjectiveDescriptionsAsync(
        List<ObjectiveMasteryDto> objectives,
        CancellationToken cancellationToken)
    {
        if (objectives.Count == 0)
        {
            return;
        }

        var codes = objectives.Select(o => o.Code).ToList();
        var descriptions = await db.LearningObjectives
            .AsNoTracking()
            .Where(o => codes.Contains(o.Code))
            .Select(o => new { o.Code, o.Description })
            .ToDictionaryAsync(o => o.Code, o => o.Description, cancellationToken);

        for (var i = 0; i < objectives.Count; i++)
        {
            if (descriptions.TryGetValue(objectives[i].Code, out var description))
            {
                objectives[i] = objectives[i] with { Description = description };
            }
        }
    }

    private sealed record AttemptFeedback(Guid AssessmentId, List<QuestionFeedback> Feedback);
}
