using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;
using SimuLearn.Domain.Lessons;

namespace SimuLearn.Api.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var admin = app.MapGroup("/api/v1/admin")
            .WithTags("Administration")
            .RequireAuthorization(ApiPolicies.Administrator);

        admin.MapGet("/users", async (
                string? search,
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
                Results.Ok(await adminService.ListUsersAsync(
                    http.Request.ToPageQuery(),
                    search,
                    cancellationToken)))
            .WithSummary("Search accounts");

        admin.MapPut("/users/{id:guid}/roles", async (
                Guid id,
                UpdateRolesRequest request,
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await adminService.UpdateRolesAsync(actor, id, request, cancellationToken));
            })
            .WithSummary("Replace a user's roles");

        admin.MapPost("/users/{id:guid}/status", async (
                Guid id,
                UpdateUserStatusRequest request,
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await adminService.SetStatusAsync(
                    actor,
                    id,
                    request.Status,
                    cancellationToken));
            })
            .WithSummary("Enable or disable an account");

        admin.MapGet("/content-reports", async (
                string? status,
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
            {
                ContentReportStatus? filter = Enum.TryParse<ContentReportStatus>(status, true, out var parsed)
                    ? parsed
                    : null;

                return Results.Ok(await adminService.ListReportsAsync(
                    filter,
                    http.Request.ToPageQuery(),
                    cancellationToken));
            })
            .WithSummary("Review the content moderation queue");

        admin.MapPut("/content-reports/{id:guid}", async (
                Guid id,
                ResolveContentReportRequest request,
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await adminService.ResolveReportAsync(actor, id, request, cancellationToken));
            })
            .WithSummary("Update a content report's status and notes");

        admin.MapGet("/audit", async (
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
                Results.Ok(await adminService.ListAuditAsync(
                    http.Request.ToPageQuery(),
                    cancellationToken)))
            .WithSummary("Read the administrative audit trail");

        var reports = app.MapGroup("/api/v1/content-reports")
            .WithTags("Content reports")
            .RequireAuthorization(ApiPolicies.Authenticated);

        reports.MapPost("", async (
                CreateContentReportRequest request,
                HttpContext http,
                AdminService adminService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var created = await adminService.CreateReportAsync(actor, request, cancellationToken);
                return Results.Created($"/api/v1/admin/content-reports/{created.Id}", created);
            })
            .WithSummary("Report an issue with a lesson, simulation or tutor answer");

        return app;
    }
}
