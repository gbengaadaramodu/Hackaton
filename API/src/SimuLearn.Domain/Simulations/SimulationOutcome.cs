using System.Globalization;

namespace SimuLearn.Domain.Simulations;

/// <summary>A labelled measured value produced by the deterministic model (FR-034).</summary>
public sealed record SimulationObservation(
    string Name,
    string Label,
    double Value,
    string Unit,
    string? Symbol = null,
    string? Description = null)
{
    public string Display => $"{Value.ToString("0.###", CultureInfo.InvariantCulture)} {Unit}".Trim();
}

/// <summary>One rendered frame of an animated simulation.</summary>
public sealed record TrajectoryPoint(double TimeSeconds, double PositionMetres, double VelocityMetresPerSecond);

/// <summary>
/// Authoritative result of one simulation calculation. Every number the tutor may mention comes
/// from here (HAC-006): the language model never computes physics.
/// </summary>
public sealed class SimulationOutcome
{
    public required IReadOnlyList<SimulationObservation> Observations { get; init; }

    public required IReadOnlyList<TrajectoryPoint> Trajectory { get; init; }

    /// <summary>Deterministic, plain-language statement of the calculated result.</summary>
    public required string Summary { get; init; }

    public required bool IsInMotion { get; init; }

    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>
    /// Every numeric value that is legitimately part of this result (including physical constants
    /// that were actually used). Used by the grounding guard on generated explanations.
    /// </summary>
    public IReadOnlyDictionary<string, double> GroundingValues { get; init; } =
        new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

    public SimulationObservation? Find(string name) =>
        Observations.FirstOrDefault(o => string.Equals(o.Name, name, StringComparison.OrdinalIgnoreCase));
}
