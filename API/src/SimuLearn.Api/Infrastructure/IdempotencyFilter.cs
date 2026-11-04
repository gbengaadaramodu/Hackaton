using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Platform;

namespace SimuLearn.Api.Infrastructure;

/// <summary>
/// Stores the successful response for a client-supplied <c>Idempotency-Key</c> so a retried POST
/// cannot create two sessions, apply a command twice or submit an assessment twice (SRS 8.3).
/// Reusing a key with a different body is rejected.
/// </summary>
public sealed class IdempotencyFilter(ILogger<IdempotencyFilter> logger) : IEndpointFilter
{
    public const string HeaderName = "Idempotency-Key";

    private const int MaxKeyLength = 200;
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        var key = http.Request.Headers[HeaderName].ToString().Trim();

        if (key.Length is 0 or > MaxKeyLength || http.User.Identity?.IsAuthenticated != true)
        {
            return await next(context);
        }

        var actor = http.User.ToActor();
        var endpoint = $"{http.Request.Method} {http.Request.Path}";
        var body = await ReadBodyAsync(http.Request);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body)));

        var db = http.RequestServices.GetRequiredService<IApplicationDbContext>();
        var now = DateTimeOffset.UtcNow;

        var existing = await db.IdempotencyRecords
            .SingleOrDefaultAsync(
                r => r.UserId == actor.Id && r.Endpoint == endpoint && r.IdempotencyKey == key,
                http.RequestAborted);

        if (existing is not null)
        {
            if (existing.ExpiresAt > now)
            {
                if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                {
                    return Problem(
                        StatusCodes.Status409Conflict,
                        "idempotency_key_reuse",
                        "This Idempotency-Key was already used with a different request body.");
                }

                logger.LogInformation("Replaying stored response for idempotency key {Key}.", key);

                var stored = JsonSerializer.Deserialize<JsonElement>(existing.ResponseJson, ApiJson.Options);
                return Results.Json(stored, ApiJson.Options, statusCode: existing.StatusCode);
            }

            db.IdempotencyRecords.Remove(existing);
            await db.SaveChangesAsync(http.RequestAborted);
        }

        var result = await next(context);

        if (result is IValueHttpResult { Value: not null } valueResult)
        {
            var statusCode = result is IStatusCodeHttpResult { StatusCode: { } code } ? code : StatusCodes.Status200OK;

            if (statusCode is >= 200 and < 300)
            {
                var json = JsonSerializer.Serialize(valueResult.Value, valueResult.Value.GetType(), ApiJson.Options);

                db.IdempotencyRecords.Add(new IdempotencyRecord
                {
                    UserId = actor.Id,
                    Endpoint = endpoint,
                    IdempotencyKey = key,
                    RequestHash = requestHash,
                    StatusCode = statusCode,
                    ResponseJson = json,
                    ExpiresAt = now.Add(Retention),
                });

                try
                {
                    await db.SaveChangesAsync(http.RequestAborted);
                }
                catch (DbUpdateException exception)
                {
                    // A concurrent retry already stored this key; the first stored response wins.
                    logger.LogWarning(exception, "Idempotency record for key {Key} was stored concurrently.", key);
                }
            }
        }

        return result;
    }

    private static IResult Problem(int status, string code, string detail) =>
        Results.Json(
            new
            {
                type = $"https://httpstatuses.io/{status}",
                title = "Conflict",
                status,
                detail,
                code,
            },
            ApiJson.Options,
            statusCode: status,
            contentType: "application/problem+json");

    /// <summary>
    /// Reads the raw payload. Request buffering is enabled by <see cref="CorrelationIdMiddleware"/>,
    /// which runs before the framework binds arguments, so the body is still available here.
    /// </summary>
    private static async Task<string> ReadBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanRead)
        {
            return string.Empty;
        }

        request.EnableBuffering();

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true);

        var body = await reader.ReadToEndAsync(request.HttpContext.RequestAborted);

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        return body;
    }
}
