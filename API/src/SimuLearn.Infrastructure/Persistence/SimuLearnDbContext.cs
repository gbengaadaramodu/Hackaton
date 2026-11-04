using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Analytics;
using SimuLearn.Domain.Assessments;
using SimuLearn.Domain.Assignments;
using SimuLearn.Domain.Classrooms;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Curriculum;
using SimuLearn.Domain.Identity;
using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Platform;
using SimuLearn.Domain.Requests;
using SimuLearn.Domain.Sessions;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Tutoring;

namespace SimuLearn.Infrastructure.Persistence;

/// <summary>
/// Single persistence context for the modular monolith. Aggregate boundaries are expressed by
/// separate DbSets and explicit navigation configuration rather than by distributed transactions.
/// </summary>
public sealed class SimuLearnDbContext(DbContextOptions<SimuLearnDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Institution> Institutions => Set<Institution>();

    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();

    public DbSet<ClassMember> ClassMembers => Set<ClassMember>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<CurriculumStandard> CurriculumStandards => Set<CurriculumStandard>();

    public DbSet<LearningObjective> LearningObjectives => Set<LearningObjective>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<LessonObjective> LessonObjectives => Set<LessonObjective>();

    public DbSet<ContentReview> ContentReviews => Set<ContentReview>();

    public DbSet<ContentReport> ContentReports => Set<ContentReport>();

    public DbSet<SimulationTemplate> SimulationTemplates => Set<SimulationTemplate>();

    public DbSet<SimulationSession> Sessions => Set<SimulationSession>();

    public DbSet<SessionSnapshot> SessionSnapshots => Set<SessionSnapshot>();

    public DbSet<Conversation> Conversations => Set<Conversation>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<Assessment> Assessments => Set<Assessment>();

    public DbSet<AssessmentAttempt> AssessmentAttempts => Set<AssessmentAttempt>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<SimulationRequest> SimulationRequests => Set<SimulationRequest>();

    public DbSet<LearningEvent> LearningEvents => Set<LearningEvent>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Institution>(entity =>
        {
            entity.HasIndex(i => i.Name).IsUnique();
            entity.Property(i => i.Name).HasMaxLength(160).IsRequired();
            entity.Property(i => i.CountryCode).HasMaxLength(2).IsRequired();
            entity.Property(i => i.ConfigurationJson).IsRequired();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.NormalizedEmail).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(254).IsRequired();
            entity.Property(u => u.NormalizedEmail).HasMaxLength(254).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(120).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(400).IsRequired();
            entity.Property(u => u.GuardianEmail).HasMaxLength(254);

            entity.HasOne(u => u.Institution)
                .WithMany(i => i.Users)
                .HasForeignKey(u => u.InstitutionId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(u => u.Roles)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(r => new { r.UserId, r.Role });
        });

        modelBuilder.Entity<ClassRoom>(entity =>
        {
            entity.HasIndex(c => c.JoinCode).IsUnique();
            entity.Property(c => c.Name).HasMaxLength(120).IsRequired();
            entity.Property(c => c.JoinCode).HasMaxLength(12).IsRequired();

            entity.HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.Institution)
                .WithMany()
                .HasForeignKey(c => c.InstitutionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<ClassMember>(entity =>
        {
            entity.HasKey(m => new { m.ClassRoomId, m.StudentId });

            entity.HasOne(m => m.ClassRoom)
                .WithMany(c => c.Members)
                .HasForeignKey(m => m.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Student)
                .WithMany()
                .HasForeignKey(m => m.StudentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Subject>(entity =>
        {
            entity.HasIndex(s => s.Code).IsUnique();
            entity.Property(s => s.Name).HasMaxLength(120).IsRequired();
            entity.Property(s => s.Code).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<CurriculumStandard>(entity =>
        {
            entity.HasIndex(c => new { c.Region, c.CurriculumCode, c.Version }).IsUnique();
            entity.Property(c => c.Region).HasMaxLength(80).IsRequired();
            entity.Property(c => c.CurriculumCode).HasMaxLength(80).IsRequired();
            entity.Property(c => c.Version).HasMaxLength(40).IsRequired();
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<LearningObjective>(entity =>
        {
            entity.HasIndex(o => o.Code).IsUnique();
            entity.Property(o => o.Code).HasMaxLength(80).IsRequired();
            entity.Property(o => o.Description).HasMaxLength(1000).IsRequired();
            entity.Property(o => o.Level).HasMaxLength(40).IsRequired();

            entity.HasOne(o => o.Subject)
                .WithMany(s => s.LearningObjectives)
                .HasForeignKey(o => o.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.CurriculumStandard)
                .WithMany(c => c.LearningObjectives)
                .HasForeignKey(o => o.CurriculumStandardId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Lesson>(entity =>
        {
            entity.Property(l => l.Title).HasMaxLength(200).IsRequired();
            entity.Property(l => l.Description).HasMaxLength(2000);
            entity.Property(l => l.GradeLevel).HasMaxLength(40).IsRequired();
            entity.Property(l => l.ModelKey).HasMaxLength(80).IsRequired();
            entity.Property(l => l.DraftConfigurationJson).IsRequired();
            entity.HasIndex(l => new { l.Status, l.SubjectId });

            entity.HasOne(l => l.Subject)
                .WithMany()
                .HasForeignKey(l => l.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(l => l.Objectives)
                .WithOne(o => o.Lesson)
                .HasForeignKey(o => o.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LessonObjective>(entity =>
        {
            entity.HasKey(o => new { o.LessonId, o.LearningObjectiveId });

            entity.HasOne(o => o.LearningObjective)
                .WithMany()
                .HasForeignKey(o => o.LearningObjectiveId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ContentReview>(entity =>
        {
            entity.Property(r => r.Comments).HasMaxLength(2000);
            entity.HasIndex(r => new { r.LessonId, r.LessonVersion });
            entity.HasOne(r => r.Lesson)
                .WithMany()
                .HasForeignKey(r => r.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContentReport>(entity =>
        {
            entity.Property(r => r.TargetType).HasMaxLength(40).IsRequired();
            entity.Property(r => r.Category).HasMaxLength(80).IsRequired();
            entity.Property(r => r.Description).HasMaxLength(2000).IsRequired();
            entity.Property(r => r.ResolutionNotes).HasMaxLength(2000);
            entity.HasIndex(r => r.Status);
        });

        modelBuilder.Entity<SimulationTemplate>(entity =>
        {
            entity.HasIndex(t => t.ModelKey).IsUnique();
            entity.Property(t => t.ModelKey).HasMaxLength(80).IsRequired();
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Description).HasMaxLength(1000);
            entity.Property(t => t.Domain).HasMaxLength(120).IsRequired();
            entity.Property(t => t.SchemaVersion).HasMaxLength(20).IsRequired();
        });

        modelBuilder.Entity<SimulationSession>(entity =>
        {
            entity.Property(s => s.ModelKey).HasMaxLength(80).IsRequired();
            entity.Property(s => s.InitialStateJson).IsRequired();
            entity.Property(s => s.StateJson).IsRequired();
            entity.Property(s => s.Version).IsConcurrencyToken();
            entity.HasIndex(s => new { s.UserId, s.LastActivityAt });
            entity.HasIndex(s => s.AssignmentId);

            entity.HasMany(s => s.Snapshots)
                .WithOne(s => s.Session)
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SessionSnapshot>(entity =>
        {
            entity.Property(s => s.StateJson).IsRequired();
            entity.HasIndex(s => new { s.SessionId, s.CreatedAt });
        });

        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasIndex(c => c.SessionId).IsUnique();

            entity.HasMany(c => c.Messages)
                .WithOne(m => m.Conversation)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(m => m.Content).HasMaxLength(4000).IsRequired();
            entity.Property(m => m.Provider).HasMaxLength(120);
            entity.Property(m => m.GroundingJson).HasMaxLength(8000);
            entity.HasIndex(m => new { m.ConversationId, m.CreatedAt });
        });

        modelBuilder.Entity<Assessment>(entity =>
        {
            entity.Property(a => a.Title).HasMaxLength(200).IsRequired();
            entity.Property(a => a.QuestionsJson).IsRequired();
            entity.Property(a => a.ModelKey).HasMaxLength(80);
            entity.HasIndex(a => new { a.LessonId, a.Status });
        });

        modelBuilder.Entity<AssessmentAttempt>(entity =>
        {
            entity.Property(a => a.AnswersJson).IsRequired();
            entity.Property(a => a.FeedbackJson).IsRequired();
            entity.HasIndex(a => new { a.UserId, a.SubmittedAt });
            entity.HasIndex(a => a.AssessmentId);

            entity.HasOne(a => a.Assessment)
                .WithMany()
                .HasForeignKey(a => a.AssessmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Assignment>(entity =>
        {
            entity.Property(a => a.Instructions).HasMaxLength(2000);
            entity.HasIndex(a => new { a.ClassRoomId, a.LessonId });

            entity.HasOne(a => a.ClassRoom)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Lesson)
                .WithMany()
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SimulationRequest>(entity =>
        {
            entity.Property(r => r.Prompt).HasMaxLength(2000).IsRequired();
            entity.Property(r => r.Subject).HasMaxLength(80);
            entity.Property(r => r.Level).HasMaxLength(40);
            entity.Property(r => r.Provider).HasMaxLength(120);
            entity.Property(r => r.ErrorCode).HasMaxLength(80);
            entity.Property(r => r.ErrorMessage).HasMaxLength(1000);
            entity.HasIndex(r => new { r.UserId, r.CreatedAt });
        });

        modelBuilder.Entity<LearningEvent>(entity =>
        {
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.HasIndex(e => new { e.SessionId, e.Type });
        });

        modelBuilder.Entity<AuditEvent>(entity =>
        {
            entity.Property(e => e.Action).HasMaxLength(120).IsRequired();
            entity.Property(e => e.TargetType).HasMaxLength(80).IsRequired();
            entity.Property(e => e.MetadataJson).IsRequired();
            entity.Property(e => e.CorrelationId).HasMaxLength(100);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.Property(r => r.IdempotencyKey).HasMaxLength(200).IsRequired();
            entity.Property(r => r.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(r => r.RequestHash).HasMaxLength(80).IsRequired();
            entity.Property(r => r.ResponseJson).IsRequired();
            entity.HasIndex(r => new { r.UserId, r.Endpoint, r.IdempotencyKey }).IsUnique();
        });

        if (Database.IsSqlite())
        {
            ApplySqliteDateTimeOffsetConversion(modelBuilder);
        }
    }

    /// <summary>
    /// SQLite has no native DateTimeOffset type, so timestamps are stored as UTC ticks. This keeps
    /// ordering and range comparisons translatable instead of failing at query time.
    /// </summary>
    private static void ApplySqliteDateTimeOffsetConversion(ModelBuilder modelBuilder)
    {
        var converter = new ValueConverter<DateTimeOffset, long>(
            value => value.UtcTicks,
            value => new DateTimeOffset(value, TimeSpan.Zero));

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(converter);
            }
        }
    }
}
