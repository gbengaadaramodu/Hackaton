using SimuLearn.Domain.Identity;

namespace SimuLearn.Application.Contracts;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    RoleName Role = RoleName.Student,
    DateOnly? DateOfBirth = null,
    string? GuardianEmail = null,
    bool GuardianConsent = false,
    Guid? InstitutionId = null);

public sealed record LoginRequest(string Email, string Password);

public sealed record UserProfile(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<RoleName> Roles,
    UserStatus Status,
    Guid? InstitutionId,
    string? InstitutionName,
    string? GuardianEmail,
    DateTimeOffset? GuardianConsentAt,
    DateTimeOffset CreatedAt);

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, UserProfile User);
