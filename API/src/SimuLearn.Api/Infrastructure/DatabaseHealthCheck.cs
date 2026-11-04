using Microsoft.Extensions.Diagnostics.HealthChecks;
using SimuLearn.Infrastructure.Persistence;

namespace SimuLearn.Api.Infrastructure;

/// <summary>Readiness probe: the service is only ready when the database answers.</summary>
public sealed class DatabaseHealthCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SimuLearnDbContext>();

            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database reachable.")
                : HealthCheckResult.Unhealthy("Database did not accept a connection.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database check failed.", exception);
        }
    }
}
