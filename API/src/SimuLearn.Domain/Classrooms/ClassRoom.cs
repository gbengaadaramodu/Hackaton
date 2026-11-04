using SimuLearn.Domain.Assignments;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Identity;

namespace SimuLearn.Domain.Classrooms;

/// <summary>
/// Named <c>ClassRoom</c> because <c>Class</c> is a C# keyword. A class belongs to one teacher
/// within an institution and students join it with a short join code (FR-003, FR-004).
/// </summary>
public sealed class ClassRoom : Entity
{
    public required string Name { get; set; }

    public Guid? InstitutionId { get; set; }

    public Institution? Institution { get; set; }

    public Guid TeacherId { get; set; }

    public User? Teacher { get; set; }

    public required string JoinCode { get; set; }

    public bool IsActive { get; set; } = true;

    public List<ClassMember> Members { get; set; } = [];

    public List<Assignment> Assignments { get; set; } = [];
}

public sealed class ClassMember
{
    public Guid ClassRoomId { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public Guid StudentId { get; set; }

    public User? Student { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
