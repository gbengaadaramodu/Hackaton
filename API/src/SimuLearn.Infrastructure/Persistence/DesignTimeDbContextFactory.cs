using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SimuLearn.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> so migrations can be generated without booting the API host.
/// Set <c>SIMULEARN_CONNECTION</c> to target another database when scaffolding.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SimuLearnDbContext>
{
    public SimuLearnDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("SIMULEARN_CONNECTION")
                         ?? "Data Source=simulearn.db";

        var options = new DbContextOptionsBuilder<SimuLearnDbContext>()
            .UseSqlite(connection)
            .Options;

        return new SimuLearnDbContext(options);
    }
}
