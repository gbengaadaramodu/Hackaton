using SimuLearn.Domain.Identity;
using SimuLearn.Domain.Lessons;

namespace SimuLearn.Application.Contracts;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    UserStatus Status,
    IReadOnlyList<RoleName> Roles,
    Guid? InstitutionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignInAt);

public sealed record UpdateRolesRequest(IReadOnlyList<RoleName> Roles);

public sealed record UpdateUserStatusRequest(UserStatus Status);

public sealed record CreateContentReportRequest(
    string TargetType,
    Guid? TargetId,
    string Category,
    string Description);

public sealed record ContentReportDto(
    Guid Id,
    Guid ReportedByUserId,
    string TargetType,
    Guid? TargetId,
    string Category,
    string Description,
    ContentReportStatus Status,
    string? ResolutionNotes,
    DateTimeOffset CreatedAt);

public sealed record ResolveContentReportRequest(
    ContentReportStatus Status,
    string? ResolutionNotes = null);

public sealed record AuditEventDto(
    Guid Id,
    Guid? ActorId,
    string Action,
    string TargetType,
    Guid? TargetId,
    string MetadataJson,
    string? CorrelationId,
    DateTimeOffset CreatedAt);
