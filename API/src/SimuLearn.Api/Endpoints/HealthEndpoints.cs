using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimuLearn.Api.Infrastructure;

namespace SimuLearn.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/api/v1/health", new HealthCheckOptions
            {
                ResponseWriter = WriteAsync,
            })
            .AllowAnonymous()
            .WithTags("Health")
            .WithSummary("Basic service and database health");

        app.MapGet("/", () => Results.Ok(new
            {
                service = "SimuLearn API",
                version = "1.0.0",
                documentation = "/scalar/v1",
                health = "/api/v1/health",
                api = "/api/v1",
            }))
            .AllowAnonymous();

        return app;
    }

    private static async Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var payload = new
        {
            status = report.Status.ToString(),
            durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
            }),
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, ApiJson.Options));
    }
}
