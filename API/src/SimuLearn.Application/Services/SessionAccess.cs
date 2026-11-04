using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Sessions;

namespace SimuLearn.Application.Services;

/// <summary>
/// Single place that decides who may observe or change a session. Ownership is always checked on the
/// server; identifiers from the client are never trusted (SEC-001, SEC-008, SEC-009).
/// </summary>
internal static class SessionAccess
{
    public static async Task<bool> CanViewAsync(
        IApplicationDbContext db,
        Actor actor,
        SimulationSession session,
        CancellationToken cancellationToken)
    {
        if (session.UserId == actor.Id)
        {
            return true;
        }

        if (actor.IsAdministratorOrReviewer)
        {
            return true;
        }

        if (!actor.IsTeacher)
        {
            return false;
        }

        if (session.LessonId is { } lessonId)
        {
            var ownsLesson = await db.Lessons
                .AnyAsync(l => l.Id == lessonId && l.AuthorId == actor.Id, cancellationToken);
            if (ownsLesson)
            {
                return true;
            }
        }

        if (session.AssignmentId is { } assignmentId)
        {
            return await db.Assignments
                .AnyAsync(
                    a => a.Id == assignmentId && a.ClassRoom != null && a.ClassRoom.TeacherId == actor.Id,
                    cancellationToken);
        }

        return false;
    }

    public static async Task<bool> CanManageAsync(
        IApplicationDbContext db,
        Actor actor,
        SimulationSession session,
        CancellationToken cancellationToken)
    {
        if (session.UserId == actor.Id || actor.IsAdministrator)
        {
            return true;
        }

        return actor.IsTeacher && await CanViewAsync(db, actor, session, cancellationToken);
    }
}
