using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Contracts;
using SimuLearn.Domain.Assignments;
using SimuLearn.Domain.Classrooms;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Sessions;

namespace SimuLearn.Application.Services;

/// <summary>
/// Class membership and assignments. Join codes grant access to one class only; a teacher can never
/// reach a class they do not own, and a student only sees assignments for classes they joined
/// (FR-003, FR-004, SEC-009).
/// </summary>
public sealed class ClassroomService(IApplicationDbContext db, ILearningEventRecorder events)
{
    private const string JoinCodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int JoinCodeLength = 6;

    public async Task<ClassDto> CreateAsync(
        Actor actor,
        CreateClassRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsTeacher && !actor.IsAdministrator)
        {
            throw new ForbiddenException("Only teachers can create classes.");
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (name.Length is < 3 or > 120)
        {
            throw Validation("name", "Use a class name between 3 and 120 characters.");
        }

        if (request.InstitutionId is { } institutionId
            && !await db.Institutions.AnyAsync(i => i.Id == institutionId, cancellationToken))
        {
            throw new NotFoundException("Institution", institutionId);
        }

        var classRoom = new ClassRoom
        {
            Name = name,
            TeacherId = actor.Id,
            InstitutionId = request.InstitutionId,
            JoinCode = await GenerateJoinCodeAsync(cancellationToken),
        };

        db.ClassRooms.Add(classRoom);
        await db.SaveChangesAsync(cancellationToken);

        events.Audit(actor, "class.created", "class", classRoom.Id);
        await db.SaveChangesAsync(cancellationToken);

        return await BuildAsync(classRoom.Id, cancellationToken);
    }

    public async Task<ClassDto> JoinAsync(Actor actor, JoinClassRequest request, CancellationToken cancellationToken)
    {
        if (!actor.IsStudent)
        {
            throw new ForbiddenException("Only students can join a class with a join code.");
        }

        var code = (request.JoinCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.Length != JoinCodeLength)
        {
            throw Validation("joinCode", "Enter the 6-character join code from your teacher.");
        }

        var classRoom = await db.ClassRooms
            .SingleOrDefaultAsync(c => c.JoinCode == code && c.IsActive, cancellationToken)
            ?? throw new NotFoundException("Class", code);

        var existing = await db.ClassMembers
            .AnyAsync(m => m.ClassRoomId == classRoom.Id && m.StudentId == actor.Id, cancellationToken);

        if (!existing)
        {
            db.ClassMembers.Add(new ClassMember
            {
                ClassRoomId = classRoom.Id,
                StudentId = actor.Id,
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        return await BuildAsync(classRoom.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassDto>> ListAsync(Actor actor, CancellationToken cancellationToken)
    {
        IQueryable<ClassRoom> query = db.ClassRooms.AsNoTracking().Where(c => c.IsActive);

        if (actor.IsAdministratorOrReviewer)
        {
            // Full visibility.
        }
        else if (actor.IsTeacher)
        {
            query = query.Where(c => c.TeacherId == actor.Id);
        }
        else
        {
            var memberships = db.ClassMembers.Where(m => m.StudentId == actor.Id).Select(m => m.ClassRoomId);
            query = query.Where(c => memberships.Contains(c.Id));
        }

        var classes = await query
            .Include(c => c.Teacher)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        var counts = await db.ClassMembers
            .Where(m => classes.Select(c => c.Id).Contains(m.ClassRoomId))
            .GroupBy(m => m.ClassRoomId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return classes
            .Select(c => ToDto(c, counts.FirstOrDefault(x => x.Key == c.Id)?.Count ?? 0))
            .ToList();
    }

    public async Task<ClassDto> GetAsync(Actor actor, Guid id, CancellationToken cancellationToken)
    {
        var classRoom = await LoadClassAsync(id, cancellationToken);
        await EnsureCanViewAsync(actor, classRoom, cancellationToken);
        return await BuildAsync(id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClassMemberDto>> MembersAsync(
        Actor actor,
        Guid id,
        CancellationToken cancellationToken)
    {
        var classRoom = await LoadClassAsync(id, cancellationToken);
        await EnsureCanViewAsync(actor, classRoom, cancellationToken);

        // The roster exposes classmates' names and email addresses, which students do not need
        // (child-protection guidance: restrict student-to-student communication).
        if (!actor.IsTeacher && !actor.IsAdministrator)
        {
            throw new ForbiddenException("Only teachers and administrators can view the class roster.");
        }

        return await db.ClassMembers
            .AsNoTracking()
            .Where(m => m.ClassRoomId == id)
            .OrderBy(m => m.Student!.DisplayName)
            .Select(m => new ClassMemberDto(
                m.StudentId,
                m.Student!.DisplayName,
                m.Student.Email,
                m.JoinedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<AssignmentDto> CreateAssignmentAsync(
        Actor actor,
        CreateAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        if (!actor.IsTeacher && !actor.IsAdministrator)
        {
            throw new ForbiddenException("Only teachers can create assignments.");
        }

        var classRoom = await LoadClassAsync(request.ClassId, cancellationToken);
        if (classRoom.TeacherId != actor.Id && !actor.IsAdministrator)
        {
            throw new ForbiddenException("You can only assign work to your own classes.");
        }

        var lesson = await db.Lessons
            .AsNoTracking()
            .SingleOrDefaultAsync(l => l.Id == request.LessonId, cancellationToken)
            ?? throw new NotFoundException("Lesson", request.LessonId);

        var canAssign = lesson.Status == LessonStatus.Published || lesson.AuthorId == actor.Id;
        if (!canAssign)
        {
            throw new ConflictException("Only a published lesson can be assigned.", "lesson_not_published");
        }

        if (request.DueAt is { } dueAt && dueAt < DateTimeOffset.UtcNow.AddMinutes(-1))
        {
            throw Validation("dueAt", "Choose a due date in the future.");
        }

        var assignment = new Assignment
        {
            ClassRoomId = classRoom.Id,
            LessonId = lesson.Id,
            TeacherId = actor.Id,
            Instructions = request.Instructions?.Trim(),
            DueAt = request.DueAt,
        };

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(cancellationToken);

        events.Audit(actor, "assignment.created", "assignment", assignment.Id, new
        {
            classId = classRoom.Id,
            lessonId = lesson.Id,
        });
        await db.SaveChangesAsync(cancellationToken);

        return new AssignmentDto(
            assignment.Id,
            classRoom.Id,
            classRoom.Name,
            lesson.Id,
            lesson.Title,
            lesson.ModelKey,
            assignment.Instructions,
            assignment.DueAt,
            assignment.TeacherId,
            assignment.CreatedAt);
    }

    public async Task<PagedResult<AssignmentDto>> ListAssignmentsAsync(
        Actor actor,
        Guid? classId,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        IQueryable<Assignment> query = db.Assignments
            .AsNoTracking()
            .Include(a => a.ClassRoom)
            .Include(a => a.Lesson);

        if (actor.IsAdministratorOrReviewer)
        {
            // Full visibility.
        }
        else if (actor.IsTeacher)
        {
            query = query.Where(a => a.TeacherId == actor.Id);
        }
        else
        {
            var memberships = db.ClassMembers.Where(m => m.StudentId == actor.Id).Select(m => m.ClassRoomId);
            query = query.Where(a => memberships.Contains(a.ClassRoomId));
        }

        if (classId is { } filter)
        {
            query = query.Where(a => a.ClassRoomId == filter);
        }

        var total = await query.CountAsync(cancellationToken);
        var assignments = await query
            .OrderBy(a => a.DueAt == null)
            .ThenBy(a => a.DueAt)
            .ThenByDescending(a => a.CreatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        var items = new List<AssignmentDto>(assignments.Count);
        foreach (var assignment in assignments)
        {
            Guid? lastSessionId = null;
            double? bestPercentage = null;
            var completed = false;

            if (actor.IsStudent)
            {
                var lastSession = await db.Sessions
                    .AsNoTracking()
                    .Where(s => s.UserId == actor.Id && s.AssignmentId == assignment.Id)
                    .OrderByDescending(s => s.LastActivityAt)
                    .Select(s => new { s.Id, s.Status })
                    .FirstOrDefaultAsync(cancellationToken);

                lastSessionId = lastSession?.Id;
                completed = lastSession?.Status == SessionStatus.Completed;

                bestPercentage = await db.AssessmentAttempts
                    .Where(a => a.UserId == actor.Id
                                && a.Assessment != null
                                && a.Assessment.LessonId == assignment.LessonId)
                    .MaxAsync(a => (double?)a.Percentage, cancellationToken);
            }

            items.Add(new AssignmentDto(
                assignment.Id,
                assignment.ClassRoomId,
                assignment.ClassRoom?.Name ?? string.Empty,
                assignment.LessonId,
                assignment.Lesson?.Title ?? string.Empty,
                assignment.Lesson?.ModelKey ?? string.Empty,
                assignment.Instructions,
                assignment.DueAt,
                assignment.TeacherId,
                assignment.CreatedAt,
                completed,
                lastSessionId,
                bestPercentage));
        }

        return new PagedResult<AssignmentDto>(items, page.NormalizedPage, page.NormalizedPageSize, total);
    }

    private async Task<ClassRoom> LoadClassAsync(Guid id, CancellationToken cancellationToken) =>
        await db.ClassRooms
            .AsNoTracking()
            .Include(c => c.Teacher)
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken)
        ?? throw new NotFoundException("Class", id);

    private async Task EnsureCanViewAsync(Actor actor, ClassRoom classRoom, CancellationToken cancellationToken)
    {
        if (actor.IsAdministratorOrReviewer || classRoom.TeacherId == actor.Id)
        {
            return;
        }

        var isMember = await db.ClassMembers
            .AnyAsync(m => m.ClassRoomId == classRoom.Id && m.StudentId == actor.Id, cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("You do not have access to this class.");
        }
    }

    private async Task<ClassDto> BuildAsync(Guid id, CancellationToken cancellationToken)
    {
        var classRoom = await db.ClassRooms
            .AsNoTracking()
            .Include(c => c.Teacher)
            .SingleAsync(c => c.Id == id, cancellationToken);

        var count = await db.ClassMembers.CountAsync(m => m.ClassRoomId == id, cancellationToken);
        return ToDto(classRoom, count);
    }

    private static ClassDto ToDto(ClassRoom classRoom, int memberCount) =>
        new(
            classRoom.Id,
            classRoom.Name,
            classRoom.JoinCode,
            classRoom.TeacherId,
            classRoom.Teacher?.DisplayName ?? string.Empty,
            memberCount,
            classRoom.InstitutionId,
            classRoom.CreatedAt);

    private async Task<string> GenerateJoinCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = new string(
                Enumerable.Range(0, JoinCodeLength)
                    .Select(_ => JoinCodeAlphabet[RandomNumberGenerator.GetInt32(JoinCodeAlphabet.Length)])
                    .ToArray());

            if (!await db.ClassRooms.AnyAsync(c => c.JoinCode == code, cancellationToken))
            {
                return code;
            }
        }

        throw new ConflictException("Could not allocate a unique join code; try again.", "join_code_exhausted");
    }

    private static DomainValidationException Validation(string field, string message) =>
        new("The class request is not valid.", new Dictionary<string, string[]> { [field] = [message] });
}
