using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
                RegisterRequest request,
                AuthService auth,
                CancellationToken cancellationToken) =>
            {
                var response = await auth.RegisterAsync(request, cancellationToken);
                return Results.Created("/api/v1/auth/me", response);
            })
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .WithSummary("Create a student or teacher account")
            .WithDescription("Self-registration is limited to student and teacher roles. Guardian consent is " +
                             "required for learners under 13.");

        group.MapPost("/login", async (
                LoginRequest request,
                AuthService auth,
                CancellationToken cancellationToken) =>
            {
                var response = await auth.LoginAsync(request, cancellationToken);
                return Results.Ok(response);
            })
            .AllowAnonymous()
            .RequireRateLimiting("auth")
            .WithSummary("Exchange credentials for an access token");

        group.MapGet("/me", async (
                HttpContext http,
                AuthService auth,
                CancellationToken cancellationToken) =>
            {
                var actor = http.User.ToActor();
                var profile = await auth.GetProfileAsync(actor.Id, cancellationToken);
                return Results.Ok(profile);
            })
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithSummary("Read the signed-in user's profile");

        return app;
    }
}
