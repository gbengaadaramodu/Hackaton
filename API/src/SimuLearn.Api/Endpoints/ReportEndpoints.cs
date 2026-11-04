using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var reports = app.MapGroup("/api/v1/reports")
            .WithTags("Reports")
            .RequireAuthorization(ApiPolicies.Authenticated);

        reports.MapGet("/classes/{id:guid}", async (
                Guid id,
                HttpContext http,
                ReportService reportService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await reportService.GetClassReportAsync(actor, id, cancellationToken));
            })
            .WithSummary("Class progress, commonly missed questions and objective mastery");

        reports.MapGet("/students/{id:guid}", async (
                Guid id,
                HttpContext http,
                ReportService reportService,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                return Results.Ok(await reportService.GetStudentReportAsync(actor, id, cancellationToken));
            })
            .WithSummary("One learner's activity, attempts and objective mastery");

        return app;
    }
}
