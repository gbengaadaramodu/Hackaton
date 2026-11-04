using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Simulations;

/// <summary>
/// An approved simulation template. The template row is a catalogue/approval record; the
/// behaviour, parameter ranges and calculations come from the deterministic
/// <see cref="ISimulationModel"/> implementation with the same <see cref="ModelKey"/>.
/// </summary>
public sealed class SimulationTemplate : Entity
{
    public required string ModelKey { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public required string Domain { get; set; }

    public required string SchemaVersion { get; set; }

    public Guid SubjectId { get; set; }

    public Guid? CurriculumStandardId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset? ApprovedAt { get; set; }
}
