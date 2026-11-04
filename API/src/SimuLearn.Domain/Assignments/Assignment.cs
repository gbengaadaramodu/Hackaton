using SimuLearn.Domain.Classrooms;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Lessons;

namespace SimuLearn.Domain.Assignments;

public sealed class Assignment : Entity
{
    public Guid ClassRoomId { get; set; }

    public ClassRoom? ClassRoom { get; set; }

    public Guid LessonId { get; set; }

    public Lesson? Lesson { get; set; }

    public Guid TeacherId { get; set; }

    public string? Instructions { get; set; }

    public DateTimeOffset? DueAt { get; set; }

    public bool IsPublished { get; set; } = true;
}
