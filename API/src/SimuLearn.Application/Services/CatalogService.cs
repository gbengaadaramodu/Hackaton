using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Contracts;
using SimuLearn.Application.Simulation;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Services;

/// <summary>Read-only curriculum and simulation catalogue used by the student workspace.</summary>
public sealed class CatalogService(IApplicationDbContext db, SimulationModelRegistry registry)
{
    public async Task<CatalogDto> GetAsync(CancellationToken cancellationToken)
    {
        var subjects = await db.Subjects
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto(s.Id, s.Code, s.Name))
            .ToListAsync(cancellationToken);

        var objectives = await db.LearningObjectives
            .AsNoTracking()
            .Include(o => o.Subject)
            .OrderBy(o => o.Code)
            .Select(o => new ObjectiveDto(
                o.Id,
                o.Code,
                o.Description,
                o.Level,
                o.Subject != null ? o.Subject.Code : string.Empty))
            .ToListAsync(cancellationToken);

        var simulations = registry.All
            .OrderBy(m => m.Subject, StringComparer.Ordinal)
            .ThenBy(m => m.Title, StringComparer.Ordinal)
            .Select(SimulationMapper.ToTemplate)
            .ToList();

        return new CatalogDto(subjects, objectives, simulations);
    }
}
