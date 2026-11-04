using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Platform;

/// <summary>
/// Stored response for a client-supplied <c>Idempotency-Key</c> so retried POSTs do not create
/// duplicate sessions, commands or attempts (SRS 8.3).
/// </summary>
public sealed class IdempotencyRecord : Entity
{
    public Guid UserId { get; set; }

    public required string IdempotencyKey { get; set; }

    public required string Endpoint { get; set; }

    public required string RequestHash { get; set; }

    public int StatusCode { get; set; }

    public required string ResponseJson { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
