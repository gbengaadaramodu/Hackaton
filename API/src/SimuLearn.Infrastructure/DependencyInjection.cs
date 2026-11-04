using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Ai;
using SimuLearn.Application.Services;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Simulations.Models;
using SimuLearn.Infrastructure.Ai;
using SimuLearn.Infrastructure.Analytics;
using SimuLearn.Infrastructure.Persistence;
using SimuLearn.Infrastructure.Security;

namespace SimuLearn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string? contentRootPath = null)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        var connectionString = ResolveConnectionString(
            configuration.GetConnectionString("Default") ?? "Data Source=simulearn.db",
            contentRootPath);
        var provider = configuration["Database:Provider"] ?? "sqlite";

        services.AddDbContext<SimuLearnDbContext>(options =>
        {
            if (string.Equals(provider, "postgres", StringComparison.OrdinalIgnoreCase)
                || string.Equals(provider, "postgresql", StringComparison.OrdinalIgnoreCase))
            {
                options.UseNpgsql(connectionString);
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<SimuLearnDbContext>());
        services.AddScoped<ILearningEventRecorder, LearningEventRecorder>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddScoped<ITokenService, JwtTokenService>();

        services.AddHttpClient<IAiChatClient, OpenAiCompatibleChatClient>();
        services.AddHttpClient<IAiImageClient, OpenAiCompatibleImageClient>();

        services.AddSingleton<ISimulationModel, InclinedPlaneModel>();
        services.AddSingleton<ISimulationModel, DcCircuitModel>();
        services.AddSingleton<ISimulationModel, NigerianIndependenceModel>();
        services.AddSingleton<SimulationModelRegistry>();
        services.AddSingleton<SceneSpecificationValidator>();

        services.AddSingleton<DeterministicRequestInterpreter>();
        services.AddSingleton<DeterministicTutorNarrator>();
        services.AddScoped<IRequestInterpreter, AiRequestInterpreter>();
        services.AddScoped<ITutorNarrator, AiTutorNarrator>();

        services.AddScoped<AuthService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<SimulationService>();
        services.AddScoped<SessionService>();
        services.AddScoped<LessonService>();
        services.AddScoped<ClassroomService>();
        services.AddScoped<AssessmentService>();
        services.AddScoped<ReportService>();
        services.AddScoped<AdminService>();

        return services;
    }

    /// <summary>
    /// Anchors a relative SQLite file at the supplied content root so that the same database
    /// is used regardless of the process working directory.
    /// </summary>
    private static string ResolveConnectionString(string connectionString, string? contentRootPath)
    {
        if (string.IsNullOrWhiteSpace(contentRootPath) || string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource)
            || builder.DataSource.Contains("Mode=Memory", StringComparison.OrdinalIgnoreCase)
            || builder.DataSource == ":memory:"
            || Path.IsPathRooted(builder.DataSource))
        {
            return connectionString;
        }

        builder.DataSource = Path.Combine(contentRootPath, builder.DataSource);
        return builder.ConnectionString;
    }
}
