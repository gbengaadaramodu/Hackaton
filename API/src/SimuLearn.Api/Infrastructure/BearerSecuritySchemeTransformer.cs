using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace SimuLearn.Api.Infrastructure;

/// <summary>
/// Adds the JWT bearer scheme to the generated OpenAPI document so the Scalar reference UI can send
/// a token. Minimal APIs do not infer a security scheme from <c>RequireAuthorization</c>.
/// </summary>
internal sealed class BearerSecuritySchemeDocumentTransformer : IOpenApiDocumentTransformer
{
    public const string SchemeName = "bearer";

    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Access token returned by POST /api/v1/auth/login. Send it as: Bearer {token}.",
        };

        return Task.CompletedTask;
    }
}

/// <summary>
/// Marks only the endpoints that actually require a caller identity, so anonymous routes such as
/// login, registration and health do not advertise a bearer requirement they do not have.
/// </summary>
internal sealed class BearerSecurityRequirementOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;

        var requiresAuthentication = metadata.OfType<IAuthorizeData>().Any()
                                     && !metadata.OfType<IAllowAnonymous>().Any();

        if (requiresAuthentication)
        {
            operation.Security =
            [
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(BearerSecuritySchemeDocumentTransformer.SchemeName, context.Document)] = [],
                },
            ];
        }

        return Task.CompletedTask;
    }
}
