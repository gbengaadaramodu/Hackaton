using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class AssessmentEndpoints
{
    public static IEndpointRouteBuilder MapAssessmentEndpoints(this IEndpointRouteBuilder app)
    {
        var assessments = app.MapGroup("/api/v1/assessments")
            .WithTags("Assessments")
            .RequireAuthorization(ApiPolicies.Authenticated);

        assessments.MapGet("", async (
                Guid? lessonId,
                AssessmentService assessmentService,
                CancellationToken cancellationToken) =>
                Results.Ok(await assessmentService.ListPublishedAsync(lessonId, cancellationToken)))
            .WithSummary("List published assessments, optionally for one lesson");

        assessments.MapGet("/attempts/mine", async (
                HttpContext http,
                AssessmentService assessmentService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await assessmentService.ListAttemptsAsync(
                    actor,
                    http.Request.ToPageQuery(),
                    cancellationToken));
            })
            .WithSummary("List the caller's submitted attempts");

        assessments.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext http,
                AssessmentService assessmentService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await assessmentService.GetAsync(actor, id, cancellationToken));
            })
            .WithSummary("Read an assessment without its answer key");

        assessments.MapPost("/{id:guid}/attempts", async (
                Guid id,
                SubmitAttemptRequest request,
                HttpContext http,
                AssessmentService assessmentService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await assessmentService.SubmitAsync(actor, id, request, cancellationToken));
            })
            .AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Submit answers for deterministic scoring and feedback");

        return app;
    }
}
