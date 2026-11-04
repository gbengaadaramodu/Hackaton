using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;
using SimuLearn.Domain.Lessons;

namespace SimuLearn.Api.Endpoints;

public static class LessonEndpoints
{
    public static IEndpointRouteBuilder MapLessonEndpoints(this IEndpointRouteBuilder app)
    {
        var lessons = app.MapGroup("/api/v1/lessons")
            .WithTags("Lessons")
            .RequireAuthorization(ApiPolicies.Authenticated);

        lessons.MapPost("", async (
                CreateLessonRequest request,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var lesson = await lessonService.CreateAsync(actor, request, cancellationToken);
                return Results.Created($"/api/v1/lessons/{lesson.Id}", lesson);
            })
            .RequireAuthorization(ApiPolicies.ContentAuthor)
            .WithSummary("Create a lesson draft from an approved simulation");

        lessons.MapGet("", async (
                string? status,
                string? subjectCode,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                LessonStatus? filter = Enum.TryParse<LessonStatus>(status, ignoreCase: true, out var parsed)
                    ? parsed
                    : null;

                return Results.Ok(await lessonService.ListAsync(
                    actor,
                    filter,
                    subjectCode,
                    http.Request.ToPageQuery(),
                    cancellationToken));
            })
            .WithSummary("List published lessons and, for teachers, their own drafts");

        lessons.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await lessonService.GetAsync(actor, id, cancellationToken));
            })
            .WithSummary("Read a lesson and its draft and published scenes");

        lessons.MapPut("/{id:guid}", async (
                Guid id,
                UpdateLessonRequest request,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await lessonService.UpdateAsync(actor, id, request, cancellationToken));
            })
            .WithSummary("Update an authorised lesson draft");

        lessons.MapPost("/{id:guid}/submit", async (
                Guid id,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await lessonService.SubmitForReviewAsync(actor, id, cancellationToken));
            })
            .WithSummary("Submit the current draft for content review");

        lessons.MapPost("/{id:guid}/review", async (
                Guid id,
                ReviewLessonRequest request,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await lessonService.ReviewAsync(actor, id, request, cancellationToken));
            })
            .RequireAuthorization(ApiPolicies.Reviewer)
            .WithSummary("Approve, request changes or reject a lesson");

        lessons.MapPost("/{id:guid}/publish", async (
                Guid id,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await lessonService.PublishAsync(actor, id, cancellationToken));
            })
            .WithSummary("Publish the approved draft as an immutable lesson version");

        lessons.MapPost("/{id:guid}/duplicate", async (
                Guid id,
                HttpContext http,
                LessonService lessonService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Created(
                    $"/api/v1/lessons/{id}",
                    await lessonService.DuplicateAsync(actor, id, cancellationToken));
            })
            .RequireAuthorization(ApiPolicies.ContentAuthor)
            .WithSummary("Duplicate a lesson into a new draft");

        lessons.MapGet("/{id:guid}/assessments", async (
                Guid id,
                HttpContext http,
                AssessmentService assessmentService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await assessmentService.ListForLessonAsync(actor, id, cancellationToken));
            })
            .WithSummary("List assessments attached to a lesson");

        return app;
    }
}
