namespace SimuLearn.Domain.Common;

/// <summary>
/// Base type for persisted domain entities. Identifiers are UUIDv7 values so they sort
/// chronologically, which keeps index locality reasonable for append-heavy tables.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
