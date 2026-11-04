using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class SessionEndpoints
{
    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var sessions = app.MapGroup("/api/v1/sessions")
            .WithTags("Simulation sessions")
            .RequireAuthorization(ApiPolicies.Authenticated);

        sessions.MapPost("", async (
                StartSessionRequest request,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var session = await sessionService.StartAsync(actor, request, cancellationToken);
                return Results.Created($"/api/v1/sessions/{session.Id}", session);
            })
            .AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Start a learning session from a template, a generated scene, a lesson or an assignment");

        sessions.MapGet("", async (
                Guid? lessonId,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var page = http.Request.ToPageQuery();
                return Results.Ok(await sessionService.ListAsync(actor, lessonId, page, cancellationToken));
            })
            .WithSummary("List sessions visible to the caller");

        sessions.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.GetAsync(actor, id, cancellationToken));
            })
            .WithSummary("Read authoritative session state");

        sessions.MapPost("/{id:guid}/commands", async (
                Guid id,
                SubmitCommandRequest request,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.SubmitCommandAsync(actor, id, request, cancellationToken));
            })
            .AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Apply a validated parameter change")
            .WithDescription("Accepts a structured set-parameter command or natural-language text. The server " +
                             "enforces the published range and optimistic concurrency.");

        sessions.MapPost("/{id:guid}/messages", async (
                Guid id,
                TutorMessageRequest request,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.AskTutorAsync(actor, id, request, cancellationToken));
            })
            .RequireRateLimiting("ai")
            .AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Ask the grounded tutor a question about the current simulation");

        sessions.MapGet("/{id:guid}/messages", async (
                Guid id,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.GetMessagesAsync(actor, id, cancellationToken));
            })
            .WithSummary("Read the session conversation");

        sessions.MapPost("/{id:guid}/reset", async (
                Guid id,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.ResetAsync(actor, id, cancellationToken));
            })
            .WithSummary("Return the simulation to its initial state");

        sessions.MapPost("/{id:guid}/status", async (
                Guid id,
                SimulationStatusRequest request,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.ChangeStatusAsync(
                    actor,
                    id,
                    request.Status,
                    cancellationToken));
            })
            .WithSummary("Pause, resume, complete or abandon a session");

        sessions.MapPost("/{id:guid}/comparisons", async (
                Guid id,
                ComparisonRequest request,
                HttpContext http,
                SessionService sessionService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await sessionService.CompareAsync(actor, id, request, cancellationToken));
            })
            .WithSummary("Compare a candidate state with the session's starting state");

        return app;
    }
}
