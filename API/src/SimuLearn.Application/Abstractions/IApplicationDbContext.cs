using Microsoft.EntityFrameworkCore;
using SimuLearn.Domain.Analytics;
using SimuLearn.Domain.Assessments;
using SimuLearn.Domain.Assignments;
using SimuLearn.Domain.Classrooms;
using SimuLearn.Domain.Curriculum;
using SimuLearn.Domain.Identity;
using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Platform;
using SimuLearn.Domain.Requests;
using SimuLearn.Domain.Sessions;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Tutoring;

namespace SimuLearn.Application.Abstractions;

/// <summary>
/// Persistence boundary for the application layer. A single context keeps the modular monolith
/// transactional; modules are separated by folders and aggregate boundaries rather than by
/// microservices (SRS 3.2).
/// </summary>
public interface IApplicationDbContext
{
    DbSet<User> Users { get; }

    DbSet<UserRole> UserRoles { get; }

    DbSet<Institution> Institutions { get; }

    DbSet<ClassRoom> ClassRooms { get; }

    DbSet<ClassMember> ClassMembers { get; }

    DbSet<Subject> Subjects { get; }

    DbSet<CurriculumStandard> CurriculumStandards { get; }

    DbSet<LearningObjective> LearningObjectives { get; }

    DbSet<Lesson> Lessons { get; }

    DbSet<LessonObjective> LessonObjectives { get; }

    DbSet<ContentReview> ContentReviews { get; }

    DbSet<ContentReport> ContentReports { get; }

    DbSet<SimulationTemplate> SimulationTemplates { get; }

    DbSet<SimulationSession> Sessions { get; }

    DbSet<SessionSnapshot> SessionSnapshots { get; }

    DbSet<Conversation> Conversations { get; }

    DbSet<Message> Messages { get; }

    DbSet<Assessment> Assessments { get; }

    DbSet<AssessmentAttempt> AssessmentAttempts { get; }

    DbSet<Assignment> Assignments { get; }

    DbSet<SimulationRequest> SimulationRequests { get; }

    DbSet<LearningEvent> LearningEvents { get; }

    DbSet<AuditEvent> AuditEvents { get; }

    DbSet<IdempotencyRecord> IdempotencyRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
