using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Common;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Simulation;
using SimuLearn.Domain.Classrooms;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Curriculum;
using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Services;

/// <summary>
/// Lesson lifecycle: draft → review → published. The published configuration is immutable until a
/// new draft is approved, so an in-progress assignment never changes underneath a student
/// (BR-002, BR-010, FR-017, FR-066).
/// </summary>
public sealed class LessonService(
    IApplicationDbContext db,
    SimulationModelRegistry registry,
    ILearningEventRecorder events)
{
    public async Task<LessonDto> CreateAsync(
        Actor actor,
        CreateLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.CanAuthorLessons)
        {
            throw new ForbiddenException("Only teachers, reviewers and administrators can create lessons.");
        }

        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is < 3 or > 200)
        {
            errors["title"] = ["Use a title between 3 and 200 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.GradeLevel))
        {
            errors["gradeLevel"] = ["Choose a grade level."];
        }

        var subject = await db.Subjects
            .SingleOrDefaultAsync(s => s.Code == request.SubjectCode && s.IsActive, cancellationToken);
        if (subject is null)
        {
            errors["subjectCode"] = [$"Unknown subject '{request.SubjectCode}'."];
        }

        if (!registry.TryGet(request.ModelKey, out var model))
        {
            errors["modelKey"] = [$"Unknown simulation model '{request.ModelKey}'."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("The lesson is not valid.", errors);
        }

        var state = ValidateState(model!, request.InitialState);
        var objectives = await ResolveObjectivesAsync(request.ObjectiveCodes, subject!.Id, cancellationToken);

        var lesson = new Lesson
        {
            Title = title,
            Description = Trim(request.Description, 2000),
            SubjectId = subject.Id,
            AuthorId = actor.Id,
            GradeLevel = request.GradeLevel!.Trim(),
            ModelKey = model!.ModelKey,
            Status = LessonStatus.Draft,
            DraftConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(state.ToDictionary())),
            DraftVersion = 1,
            Objectives = objectives.Select(o => new LessonObjective { LearningObjectiveId = o.Id }).ToList(),
        };

        db.Lessons.Add(lesson);
        await db.SaveChangesAsync(cancellationToken);

        events.Audit(actor, "lesson.created", "lesson", lesson.Id, new { lesson.ModelKey });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> UpdateAsync(
        Actor actor,
        Guid id,
        UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons
            .Include(l => l.Objectives)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        EnsureCanEdit(actor, lesson);

        if (lesson.Status == LessonStatus.Archived)
        {
            throw new ConflictException("An archived lesson cannot be edited.", "lesson_archived");
        }

        if (request.Title is not null)
        {
            var title = request.Title.Trim();
            if (title.Length is < 3 or > 200)
            {
                throw Validation("title", "Use a title between 3 and 200 characters.");
            }

            lesson.Title = title;
        }

        if (request.Description is not null)
        {
            lesson.Description = Trim(request.Description, 2000);
        }

        if (request.GradeLevel is not null)
        {
            lesson.GradeLevel = request.GradeLevel.Trim();
        }

        if (request.SubjectCode is not null)
        {
            var subject = await db.Subjects
                .SingleOrDefaultAsync(s => s.Code == request.SubjectCode && s.IsActive, cancellationToken)
                ?? throw Validation("subjectCode", $"Unknown subject '{request.SubjectCode}'.");
            lesson.SubjectId = subject.Id;
        }

        var model = registry.GetRequired(lesson.ModelKey);

        if (request.InitialState is not null)
        {
            var state = ValidateState(model, request.InitialState);
            lesson.DraftConfigurationJson =
                JsonDefaults.Serialize(LessonConfiguration.FromState(state.ToDictionary()));
        }

        if (request.ObjectiveCodes is not null)
        {
            var current = await db.Lessons
                .Where(l => l.Id == lesson.Id)
                .Select(l => l.SubjectId)
                .SingleAsync(cancellationToken);

            var objectives = await ResolveObjectivesAsync(request.ObjectiveCodes, current, cancellationToken);
            lesson.Objectives.Clear();
            foreach (var objective in objectives)
            {
                lesson.Objectives.Add(new LessonObjective { LearningObjectiveId = objective.Id });
            }
        }

        lesson.DraftVersion++;
        lesson.UpdatedAt = DateTimeOffset.UtcNow;

        if (lesson.Status == LessonStatus.InReview)
        {
            // Any edit invalidates the pending review.
            lesson.Status = LessonStatus.Draft;
        }

        await db.SaveChangesAsync(cancellationToken);

        events.Audit(actor, "lesson.updated", "lesson", lesson.Id, new { lesson.DraftVersion });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> SubmitForReviewAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons.SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        EnsureCanEdit(actor, lesson);

        if (lesson.Status is LessonStatus.Archived)
        {
            throw new ConflictException("An archived lesson cannot be submitted.", "lesson_archived");
        }

        if (lesson.Status == LessonStatus.Published && lesson.PublishedVersion == lesson.DraftVersion)
        {
            throw new ConflictException("There are no unpublished changes to review.", "nothing_to_review");
        }

        lesson.Status = LessonStatus.InReview;
        lesson.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        events.Audit(actor, "lesson.submitted_for_review", "lesson", lesson.Id, new { lesson.DraftVersion });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> ReviewAsync(
        Actor actor,
        Guid id,
        ReviewLessonRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsAdministratorOrReviewer)
        {
            throw new ForbiddenException("Only content reviewers and administrators can review lessons.");
        }

        var lesson = await db.Lessons.SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        if (lesson.Status != LessonStatus.InReview)
        {
            throw new ConflictException("Only a lesson awaiting review can be reviewed.", "not_in_review");
        }

        if (!Enum.IsDefined(request.Decision))
        {
            throw Validation("decision", "Unknown review decision.");
        }

        if (request.Decision != ReviewDecision.Approved && string.IsNullOrWhiteSpace(request.Comments))
        {
            throw Validation("comments", "Explain what must change or why the lesson was rejected.");
        }

        db.ContentReviews.Add(new ContentReview
        {
            LessonId = lesson.Id,
            ReviewerId = actor.Id,
            Decision = request.Decision,
            LessonVersion = lesson.DraftVersion,
            Comments = Trim(request.Comments, 2000),
        });

        // An approval is recorded as a review row for this draft version; the lesson only leaves
        // the review queue when the author publishes the approved version.
        if (request.Decision != ReviewDecision.Approved)
        {
            lesson.Status = LessonStatus.Draft;
        }

        lesson.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        events.Audit(actor, "lesson.reviewed", "lesson", lesson.Id, new
        {
            decision = request.Decision.ToString(),
            version = lesson.DraftVersion,
        });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> PublishAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons.SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        var isOwner = lesson.AuthorId == actor.Id;
        if (!isOwner && !actor.IsAdministrator)
        {
            throw new ForbiddenException("Only the author or an administrator can publish this lesson.");
        }

        if (lesson.Status == LessonStatus.Archived)
        {
            throw new ConflictException("An archived lesson cannot be published.", "lesson_archived");
        }

        var approved = await db.ContentReviews.AnyAsync(
            r => r.LessonId == lesson.Id
                 && r.LessonVersion == lesson.DraftVersion
                 && r.Decision == ReviewDecision.Approved,
            cancellationToken);

        if (!approved && !actor.IsAdministrator)
        {
            throw new ForbiddenException(
                "This version has not been approved by a content reviewer yet. Submit it for review first.");
        }

        lesson.PublishedConfigurationJson = lesson.DraftConfigurationJson;
        lesson.PublishedVersion = lesson.DraftVersion;
        lesson.Status = LessonStatus.Published;
        lesson.PublishedAt = DateTimeOffset.UtcNow;
        lesson.PublishedByUserId = actor.Id;
        lesson.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        events.Audit(actor, "lesson.published", "lesson", lesson.Id, new { version = lesson.PublishedVersion });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(lesson.Id, cancellationToken);
    }

    public async Task<LessonDto> DuplicateAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var source = await db.Lessons
            .AsNoTracking()
            .Include(l => l.Objectives)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        if (!actor.CanAuthorLessons)
        {
            throw new ForbiddenException("Only teachers, reviewers and administrators can duplicate lessons.");
        }

        if (source.Status != LessonStatus.Published && source.AuthorId != actor.Id && !actor.IsAdministrator)
        {
            throw new ForbiddenException("You can only duplicate published lessons or your own drafts.");
        }

        var copy = new Lesson
        {
            Title = source.Title.Length > 190 ? $"{source.Title[..190]} (copy)" : $"{source.Title} (copy)",
            Description = source.Description,
            SubjectId = source.SubjectId,
            AuthorId = actor.Id,
            GradeLevel = source.GradeLevel,
            ModelKey = source.ModelKey,
            Status = LessonStatus.Draft,
            DraftConfigurationJson = source.PublishedConfigurationJson ?? source.DraftConfigurationJson,
            DraftVersion = 1,
            Objectives = source.Objectives
                .Select(o => new LessonObjective { LearningObjectiveId = o.LearningObjectiveId })
                .ToList(),
        };

        db.Lessons.Add(copy);
        await db.SaveChangesAsync(cancellationToken);

        events.Audit(actor, "lesson.duplicated", "lesson", copy.Id, new { sourceLessonId = source.Id });
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(copy.Id, cancellationToken);
    }

    public async Task<LessonDto> GetAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        var canViewDraft = lesson.AuthorId == actor.Id || actor.IsAdministratorOrReviewer;
        if (lesson.Status != LessonStatus.Published && !canViewDraft)
        {
            throw new NotFoundException("Lesson", id);
        }

        return await BuildAsync(id, cancellationToken);
    }

    public async Task<PagedResult<LessonDto>> ListAsync(
        Actor actor,
        LessonStatus? status,
        string? subjectCode,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        IQueryable<Lesson> query = db.Lessons.AsNoTracking();

        if (actor.IsAdministratorOrReviewer)
        {
            // Full visibility for review.
        }
        else if (actor.IsTeacher)
        {
            query = query.Where(l => l.Status == LessonStatus.Published || l.AuthorId == actor.Id);
        }
        else
        {
            query = query.Where(l => l.Status == LessonStatus.Published);
        }

        if (status is { } filter)
        {
            query = query.Where(l => l.Status == filter);
        }

        if (!string.IsNullOrWhiteSpace(subjectCode))
        {
            query = query.Where(l => l.Subject != null && l.Subject.Code == subjectCode);
        }

        var total = await query.CountAsync(cancellationToken);
        var lessons = await query
            .OrderByDescending(l => l.UpdatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = new List<LessonDto>(lessons.Count);
        foreach (var lesson in lessons)
        {
            items.Add(await BuildAsync(lesson.Id, cancellationToken));
        }

        return new PagedResult<LessonDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    private async Task<LessonDto> BuildAsync(Guid id, CancellationToken cancellationToken)
    {
        var lesson = await db.Lessons
            .AsNoTracking()
            .Include(l => l.Subject)
            .SingleOrDefaultAsync(l => l.Id == id, cancellationToken)
            ?? throw new NotFoundException("Lesson", id);

        var objectives = await db.LearningObjectives
            .AsNoTracking()
            .Include(o => o.Subject)
            .Where(o => db.LessonObjectives.Any(lo => lo.LessonId == id && lo.LearningObjectiveId == o.Id))
            .ToListAsync(cancellationToken);

        var model = registry.GetRequired(lesson.ModelKey);
        var draftState = ReadState(lesson, lesson.DraftConfigurationJson);
        var publishedState = lesson.PublishedConfigurationJson is null
            ? null
            : ReadState(lesson, lesson.PublishedConfigurationJson);

        return new LessonDto(
            lesson.Id,
            lesson.Title,
            lesson.Description,
            lesson.Subject?.Code ?? string.Empty,
            lesson.Subject?.Name ?? string.Empty,
            lesson.GradeLevel,
            lesson.ModelKey,
            lesson.Status,
            lesson.DraftVersion,
            lesson.PublishedVersion,
            lesson.PublishedAt,
            lesson.AuthorId,
            objectives
                .Select(o => new ObjectiveDto(
                    o.Id,
                    o.Code,
                    o.Description,
                    o.Level,
                    o.Subject?.Code ?? string.Empty))
                .ToList(),
            SceneSpecificationFactory.Create(model, draftState, "lesson-draft"),
            publishedState is null
                ? null
                : SceneSpecificationFactory.Create(model, publishedState, "lesson-published"),
            lesson.CreatedAt,
            lesson.UpdatedAt);
    }

    private static ParameterSet ReadState(Lesson lesson, string json)
    {
        var configuration = JsonDefaults.Deserialize<LessonConfiguration>(json);
        return new ParameterSet(configuration?.InitialState ?? []);
    }

    private ParameterSet ValidateState(ISimulationModel model, IReadOnlyDictionary<string, double>? state)
    {
        var parsed = new ParameterSet(state ?? new Dictionary<string, double>());
        var validation = model.Validate(parsed);
        if (!validation.IsValid)
        {
            throw new DomainValidationException(
                "The lesson configuration is not valid for this simulation.",
                validation.ToErrors());
        }

        return model.Normalize(parsed);
    }

    private async Task<List<LearningObjective>> ResolveObjectivesAsync(
        IReadOnlyList<string>? codes,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        if (codes is null || codes.Count == 0)
        {
            return [];
        }

        var distinct = codes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();

        var objectives = await db.LearningObjectives
            .Where(o => distinct.Contains(o.Code))
            .ToListAsync(cancellationToken);

        var unknown = distinct
            .Where(code => objectives.All(o => !string.Equals(o.Code, code, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (unknown.Count > 0)
        {
            throw Validation("objectiveCodes", $"Unknown learning objective(s): {string.Join(", ", unknown)}.");
        }

        var mismatched = objectives.Where(o => o.SubjectId != subjectId).ToList();
        if (mismatched.Count > 0)
        {
            throw Validation(
                "objectiveCodes",
                $"Objective(s) {string.Join(", ", mismatched.Select(o => o.Code))} belong to another subject.");
        }

        return objectives;
    }

    private static void EnsureCanEdit(Actor actor, Lesson lesson)
    {
        if (lesson.AuthorId != actor.Id && !actor.IsAdministrator)
        {
            throw new ForbiddenException("Only the author or an administrator can edit this lesson.");
        }
    }

    private static DomainValidationException Validation(string field, string message) =>
        new("The lesson is not valid.", new Dictionary<string, string[]> { [field] = [message] });

    private static string? Trim(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}
