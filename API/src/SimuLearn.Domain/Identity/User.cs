using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Identity;

public enum UserStatus
{
    Active = 0,
    Disabled = 1,
}

public enum RoleName
{
    Student = 0,
    Teacher = 1,
    ContentReviewer = 2,
    Administrator = 3,
}

public sealed class User : Entity
{
    public required string Email { get; set; }

    public required string NormalizedEmail { get; set; }

    public required string DisplayName { get; set; }

    public required string PasswordHash { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;

    /// <summary>Only stored when the product needs age-appropriate controls; never exposed publicly.</summary>
    public DateOnly? DateOfBirth { get; set; }

    /// <summary>Set when a guardian email is on file and consent has been recorded (FR-006).</summary>
    public string? GuardianEmail { get; set; }

    public DateTimeOffset? GuardianConsentAt { get; set; }

    public Guid? InstitutionId { get; set; }

    public Institution? Institution { get; set; }

    public DateTimeOffset? LastSignInAt { get; set; }

    public List<UserRole> Roles { get; set; } = [];

    public bool HasRole(RoleName role) => Roles.Exists(r => r.Role == role);

    public bool HasAnyRole(params RoleName[] roles) => Roles.Exists(r => roles.Contains(r.Role));

    public int? AgeYears(DateOnly today)
    {
        if (DateOfBirth is not { } dob)
        {
            return null;
        }

        var age = today.Year - dob.Year;
        if (dob > today.AddYears(-age))
        {
            age--;
        }

        return age;
    }
}

public sealed class UserRole
{
    public Guid UserId { get; set; }

    public RoleName Role { get; set; }
}
