using SimuLearn.Domain.Identity;

namespace SimuLearn.Application.Abstractions;

/// <summary>The authenticated caller, resolved from validated JWT claims by the API layer.</summary>
public sealed record Actor(Guid Id, IReadOnlyCollection<RoleName> Roles, string? Email = null)
{
    public bool IsAdministrator => Roles.Contains(RoleName.Administrator);

    public bool IsContentReviewer => Roles.Contains(RoleName.ContentReviewer);

    public bool IsTeacher => Roles.Contains(RoleName.Teacher);

    public bool IsStudent => Roles.Contains(RoleName.Student);

    public bool IsAdministratorOrReviewer => IsAdministrator || IsContentReviewer;

    public bool CanAuthorLessons => IsTeacher || IsContentReviewer || IsAdministrator;
}
