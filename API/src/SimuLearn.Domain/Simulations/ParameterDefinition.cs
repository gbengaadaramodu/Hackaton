namespace SimuLearn.Domain.Simulations;

/// <summary>
/// Declares one manipulable simulation parameter, including the permitted bounds that the server
/// enforces (FR-032, FR-035). The AI never widens these ranges.
/// </summary>
public sealed record ParameterDefinition(
    string Name,
    string Label,
    double Min,
    double Max,
    double Step,
    string Unit,
    double Default,
    string? Symbol = null,
    string? Description = null)
{
    private const double BoundaryTolerance = 1e-9;

    public bool Contains(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value)
        && value >= Min - BoundaryTolerance
        && value <= Max + BoundaryTolerance;

    public bool HasIntegralValuesOnly => Step >= 1d && Math.Abs(Step % 1d) < BoundaryTolerance;
}
