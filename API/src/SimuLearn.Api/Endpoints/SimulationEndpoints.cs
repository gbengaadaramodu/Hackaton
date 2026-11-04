using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class SimulationEndpoints
{
    public static IEndpointRouteBuilder MapSimulationEndpoints(this IEndpointRouteBuilder app)
    {
        var simulations = app.MapGroup("/api/v1/simulations")
            .WithTags("Simulations");

        simulations.MapGet("/templates", async (
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
                Results.Ok(await simulationsService.ListTemplatesAsync(cancellationToken)))
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("List approved simulation templates");

        simulations.MapGet("/templates/{modelKey}", async (
                string modelKey,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
                Results.Ok(await simulationsService.GetTemplateAsync(modelKey, cancellationToken)))
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("Read one simulation template, including controls and limitations");

        simulations.MapPost("/preview", async (
                PreviewSimulationRequest request,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
                Results.Ok(await simulationsService.PreviewAsync(
                    request.ModelKey,
                    request.State,
                    cancellationToken)))
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("Calculate a simulation result without creating a session");

        simulations.MapPost("/render", (
                RenderSceneRequest request,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
                Results.Ok(simulationsService.RenderScene(
                    request.ModelKey,
                    request.State,
                    cancellationToken,
                    request.Frame)))
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("Render a server-painted SVG scene for a validated state");

        simulations.MapPost("/render-image", async (
                RenderImageSceneRequest request,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
                Results.Ok(await simulationsService.RenderImageSceneAsync(
                    request.ModelKey,
                    request.State,
                    cancellationToken)))
            .RequireAuthorization(ApiPolicies.Authenticated)
            .RequireRateLimiting("ai")
            .WithSummary("Generate an image illustration, falling back to SVG when unavailable");

        var requests = app.MapGroup("/api/v1/simulation-requests")
            .WithTags("Simulation requests");

        requests.MapPost("", async (
                CreateSimulationRequest request,
                HttpContext http,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var created = await simulationsService.CreateRequestAsync(actor, request, cancellationToken);
                return Results.Created($"/api/v1/simulation-requests/{created.Id}", created);
            })
            .RequireAuthorization(ApiPolicies.Authenticated)
            .RequireRateLimiting("ai")
            .AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Turn a natural-language request into a validated scene")
            .WithDescription("The request row is stored before interpretation, so a provider failure never loses " +
                             "the learner's prompt.");

        requests.MapGet("", async (
                HttpContext http,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var page = http.Request.ToPageQuery();
                return Results.Ok(await simulationsService.ListRequestsAsync(actor, page, cancellationToken));
            })
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("List the caller's simulation requests");

        requests.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext http,
                SimulationService simulationsService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await simulationsService.GetRequestAsync(actor, id, cancellationToken));
            })
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("Read generation status and the validated scene");

        return app;
    }
}
