using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class ClassroomEndpoints
{
    public static IEndpointRouteBuilder MapClassroomEndpoints(this IEndpointRouteBuilder app)
    {
        var classes = app.MapGroup("/api/v1/classes")
            .WithTags("Classes")
            .RequireAuthorization(ApiPolicies.Authenticated);

        classes.MapPost("", async (
                CreateClassRequest request,
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var created = await classroomService.CreateAsync(actor, request, cancellationToken);
                return Results.Created($"/api/v1/classes/{created.Id}", created);
            })
            .RequireAuthorization(ApiPolicies.ContentAuthor)
            .WithSummary("Create a class and receive a join code");

        classes.MapPost("/join", async (
                JoinClassRequest request,
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await classroomService.JoinAsync(actor, request, cancellationToken));
            })
            .RequireAuthorization(ApiPolicies.Student)
            .WithSummary("Join a class using a teacher's join code");

        classes.MapGet("", async (
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await classroomService.ListAsync(actor, cancellationToken));
            })
            .WithSummary("List the caller's classes");

        classes.MapGet("/{id:guid}", async (
                Guid id,
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await classroomService.GetAsync(actor, id, cancellationToken));
            })
            .WithSummary("Read one class the caller belongs to");

        classes.MapGet("/{id:guid}/members", async (
                Guid id,
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await classroomService.MembersAsync(actor, id, cancellationToken));
            })
            .WithSummary("List class members");

        var assignments = app.MapGroup("/api/v1/assignments")
            .WithTags("Assignments")
            .RequireAuthorization(ApiPolicies.Authenticated);

        assignments.MapPost("", async (
                CreateAssignmentRequest request,
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var created = await classroomService.CreateAssignmentAsync(actor, request, cancellationToken);
                return Results.Created($"/api/v1/assignments/{created.Id}", created);
            })
            .RequireAuthorization(ApiPolicies.ContentAuthor)
            .WithSummary("Assign a published lesson to one of the caller's classes");

        assignments.MapGet("", async (
                Guid? classId,
                HttpContext http,
                ClassroomService classroomService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await classroomService.ListAssignmentsAsync(
                    actor,
                    classId,
                    http.Request.ToPageQuery(),
                    cancellationToken));
            })
            .WithSummary("List assignments for the caller, including their own progress");

        return app;
    }
}
