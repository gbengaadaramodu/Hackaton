using System.Diagnostics;

namespace SimuLearn.Api.Infrastructure;

/// <summary>
/// Ensures every request has a correlation id that appears in logs, the response header and error
/// payloads, so a support ticket can be traced end to end (SRS 8.3).
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "correlation-id";

    private const int MaxLength = 100;

    public async Task InvokeAsync(HttpContext context)
    {
        var supplied = context.Request.Headers[HeaderName].ToString().Trim();
        var correlationId = IsAcceptable(supplied) ? supplied : Activity.Current?.TraceId.ToString()
            ?? Guid.CreateVersion7().ToString("N");

        context.Items[ItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        // Minimal APIs bind the request body before endpoint filters run, so an idempotent request
        // must be buffered here to let the idempotency filter hash the payload afterwards.
        if (context.Request.Headers.ContainsKey(IdempotencyFilter.HeaderName))
        {
            context.Request.EnableBuffering();
        }

        using (logger.BeginScope(new Dictionary<string, object> { ["correlationId"] = correlationId }))
        {
            await next(context);
        }
    }

    private static bool IsAcceptable(string value) =>
        value.Length is > 0 and <= MaxLength
        && value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_' or '.');
}
