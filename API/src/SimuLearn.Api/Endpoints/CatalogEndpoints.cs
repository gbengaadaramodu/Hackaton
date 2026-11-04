using SimuLearn.Application.Services;

namespace SimuLearn.Api.Endpoints;

public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/catalog", async (CatalogService catalog, CancellationToken cancellationToken) =>
                Results.Ok(await catalog.GetAsync(cancellationToken)))
            .RequireAuthorization(ApiPolicies.Authenticated)
            .WithTags("Catalog")
            .WithSummary("Subjects, curriculum objectives and approved simulations");

        return app;
    }
}
