using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using SimuLearn.Api.Endpoints;
using SimuLearn.Api.Infrastructure;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Identity;
using SimuLearn.Infrastructure;
using SimuLearn.Infrastructure.Persistence;
using SimuLearn.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.ConfigureHttpJsonOptions(ApiJson.ConfigureHttpJsonOptions);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var tokenValidation = JwtTokenService.CreateValidationParameters(jwtOptions);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = tokenValidation;
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(ApiPolicies.Authenticated, policy => policy.RequireAuthenticatedUser())
    .AddPolicy(ApiPolicies.Student, policy => policy.RequireRole(nameof(RoleName.Student)))
    .AddPolicy(
        ApiPolicies.ContentAuthor,
        policy => policy.RequireRole(
            nameof(RoleName.Teacher),
            nameof(RoleName.ContentReviewer),
            nameof(RoleName.Administrator)))
    .AddPolicy(
        ApiPolicies.Reviewer,
        policy => policy.RequireRole(nameof(RoleName.ContentReviewer), nameof(RoleName.Administrator)))
    .AddPolicy(ApiPolicies.Administrator, policy => policy.RequireRole(nameof(RoleName.Administrator)));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173", "http://localhost:3000", "http://localhost:3001"];

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new
            {
                type = "https://httpstatuses.io/429",
                title = "Too many requests",
                status = 429,
                detail = "Slow down and try again shortly.",
                code = "rate_limited",
            },
            ApiJson.Options,
            cancellationToken);
    };

    options.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(http),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));

    options.AddPolicy("ai", http => RateLimitPartition.GetFixedWindowLimiter(
        PartitionKey(http),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeDocumentTransformer>();
    options.AddOperationTransformer<BearerSecurityRequirementOperationTransformer>();
});

var app = builder.Build();

await PrepareDatabaseAsync(app);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthEndpoints()
    .MapAuthEndpoints()
    .MapCatalogEndpoints()
    .MapSimulationEndpoints()
    .MapSessionEndpoints()
    .MapLessonEndpoints()
    .MapClassroomEndpoints()
    .MapAssessmentEndpoints()
    .MapReportEndpoints()
    .MapAdminEndpoints();

await app.RunAsync();

static string PartitionKey(HttpContext http) =>
    http.User.FindFirstValue("sub")
    ?? http.Connection.RemoteIpAddress?.ToString()
    ?? "anonymous";

static async Task PrepareDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var db = scope.ServiceProvider.GetRequiredService<SimuLearnDbContext>();

    var migrate = app.Configuration.GetValue("Database:Migrate", defaultValue: true);
    if (migrate)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("DELETE FROM \"__EFMigrationsLock\";");
        }
        catch
        {
            // Lock table doesn't exist yet on fresh DB
        }

        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied.");
    }

    var seed = app.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();
    if (!seed.Enabled)
    {
        return;
    }

    var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    if (await DatabaseSeeder.SeedAsync(db, hasher, seed, CancellationToken.None))
    {
        logger.LogInformation("Demo curriculum, lessons and accounts seeded.");
    }
}

public partial class Program;
