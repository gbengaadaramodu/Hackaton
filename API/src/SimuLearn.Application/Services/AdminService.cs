using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Contracts;
using SimuLearn.Domain.Analytics;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Identity;
using SimuLearn.Domain.Lessons;

namespace SimuLearn.Application.Services;

/// <summary>Administrative operations: roles, account status, content reports and the audit trail.</summary>
public sealed class AdminService(IApplicationDbContext db, ILearningEventRecorder events)
{
    public async Task<PagedResult<AdminUserDto>> ListUsersAsync(
        PageQuery page,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().Include(u => u.Roles).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.NormalizedEmail.Contains(term) || u.DisplayName.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.DisplayName)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AdminUserDto>(
            users.Select(ToDto).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            total);
    }

    public async Task<AdminUserDto> UpdateRolesAsync(
        Actor actor,
        Guid userId,
        UpdateRolesRequest request,
        CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        var roles = (request.Roles ?? [])
            .Distinct()
            .ToList();

        if (roles.Count == 0)
        {
            throw Validation("roles", "A user must keep at least one role.");
        }

        if (roles.Any(role => !Enum.IsDefined(role)))
        {
            throw Validation("roles", "Unknown role supplied.");
        }

        if (user.Id == actor.Id && !roles.Contains(RoleName.Administrator))
        {
            throw new ForbiddenException("You cannot remove your own administrator role.");
        }

        user.Roles.Clear();
        foreach (var role in roles)
        {
            user.Roles.Add(new UserRole { UserId = user.Id, Role = role });
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        events.Audit(actor, "user.roles_updated", "user", user.Id, new
        {
            roles = roles.Select(r => r.ToString()).ToArray(),
        });
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<AdminUserDto> SetStatusAsync(
        Actor actor,
        Guid userId,
        UserStatus status,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(status))
        {
            throw Validation("status", "Unknown account status.");
        }

        if (userId == actor.Id && status == UserStatus.Disabled)
        {
            throw new ForbiddenException("You cannot disable your own account.");
        }

        var user = await db.Users
            .Include(u => u.Roles)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new NotFoundException("User", userId);

        user.Status = status;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        events.Audit(actor, "user.status_changed", "user", user.Id, new { status = status.ToString() });
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    public async Task<ContentReportDto> CreateReportAsync(
        Actor actor,
        CreateContentReportRequest request,
        CancellationToken cancellationToken)
    {
        var targetType = request.TargetType?.Trim().ToLowerInvariant() ?? string.Empty;
        if (targetType is not ("lesson" or "simulation" or "tutor-message" or "assessment"))
        {
            throw Validation("targetType", "Target type must be lesson, simulation, tutor-message or assessment.");
        }

        if (string.IsNullOrWhiteSpace(request.Category) || request.Category.Length > 80)
        {
            throw Validation("category", "Choose a short category for the issue.");
        }

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Trim().Length > 2000)
        {
            throw Validation("description", "Describe the issue in 1 to 2000 characters.");
        }

        var report = new ContentReport
        {
            ReportedByUserId = actor.Id,
            TargetType = targetType,
            TargetId = request.TargetId,
            Category = request.Category.Trim(),
            Description = request.Description.Trim(),
        };

        db.ContentReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        events.Record(actor.Id, LearningEventType.ContentReported, payload: new
        {
            reportId = report.Id,
            targetType,
        });
        events.Audit(actor, "content.reported", "content-report", report.Id, new { targetType });
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(report);
    }

    public async Task<PagedResult<ContentReportDto>> ListReportsAsync(
        ContentReportStatus? status,
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var query = db.ContentReports.AsNoTracking().AsQueryable();
        if (status is { } filter)
        {
            query = query.Where(r => r.Status == filter);
        }

        var total = await query.CountAsync(cancellationToken);
        var reports = await query
            .OrderBy(r => r.Status)
            .ThenByDescending(r => r.CreatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ContentReportDto>(
            reports.Select(ToDto).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            total);
    }

    public async Task<ContentReportDto> ResolveReportAsync(
        Actor actor,
        Guid id,
        ResolveContentReportRequest request,
        CancellationToken cancellationToken)
    {
        var report = await db.ContentReports.SingleOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundException("Content report", id);

        if (!Enum.IsDefined(request.Status) || request.Status == ContentReportStatus.Open)
        {
            throw Validation("status", "Choose InReview or Resolved.");
        }

        report.Status = request.Status;
        report.ResolutionNotes = request.ResolutionNotes?.Trim();
        report.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        events.Audit(actor, "content_report.updated", "content-report", report.Id, new
        {
            status = request.Status.ToString(),
        });
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(report);
    }

    public async Task<PagedResult<AuditEventDto>> ListAuditAsync(
        PageQuery page,
        CancellationToken cancellationToken)
    {
        var query = db.AuditEvents.AsNoTracking();
        var total = await query.CountAsync(cancellationToken);

        var eventsPage = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEventDto>(
            eventsPage.Select(e => new AuditEventDto(
                e.Id,
                e.ActorId,
                e.Action,
                e.TargetType,
                e.TargetId,
                e.MetadataJson,
                e.CorrelationId,
                e.CreatedAt)).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            total);
    }

    private static AdminUserDto ToDto(User user) =>
        new(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Status,
            user.Roles.Select(r => r.Role).OrderBy(r => r).ToArray(),
            user.InstitutionId,
            user.CreatedAt,
            user.LastSignInAt);

    private static ContentReportDto ToDto(ContentReport report) =>
        new(
            report.Id,
            report.ReportedByUserId,
            report.TargetType,
            report.TargetId,
            report.Category,
            report.Description,
            report.Status,
            report.ResolutionNotes,
            report.CreatedAt);

    private static DomainValidationException Validation(string field, string message) =>
        new("The request is not valid.", new Dictionary<string, string[]> { [field] = [message] });
}
