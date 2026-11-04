using SimuLearn.Domain.Common;

namespace SimuLearn.Domain.Simulations;

/// <summary>
/// Single allowlist of simulation models the platform can execute. Providers may propose a model
/// key, but only keys registered here are ever resolved (SRS 4.4, FR-013, FR-015).
/// </summary>
public sealed class SimulationModelRegistry
{
    private readonly Dictionary<string, ISimulationModel> _models;

    public SimulationModelRegistry(IEnumerable<ISimulationModel> models)
    {
        _models = models.ToDictionary(m => m.ModelKey, StringComparer.OrdinalIgnoreCase);
        if (_models.Count == 0)
        {
            throw new InvalidOperationException("At least one simulation model must be registered.");
        }
    }

    public IReadOnlyCollection<ISimulationModel> All => _models.Values;

    public bool TryGet(string? modelKey, out ISimulationModel model)
    {
        if (!string.IsNullOrWhiteSpace(modelKey) && _models.TryGetValue(modelKey.Trim(), out var resolved))
        {
            model = resolved;
            return true;
        }

        model = null!;
        return false;
    }

    public ISimulationModel? Find(string? modelKey) => TryGet(modelKey, out var model) ? model : null;

    public ISimulationModel GetRequired(string? modelKey) =>
        Find(modelKey) ?? throw new NotFoundException("Simulation model", modelKey ?? "(null)");
}
