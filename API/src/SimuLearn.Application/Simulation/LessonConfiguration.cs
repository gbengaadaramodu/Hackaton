namespace SimuLearn.Application.Simulation;

/// <summary>
/// Persisted lesson configuration. Only a validated initial state is stored; a lesson can never
/// carry executable content or a different simulation model (FR-017, BR-010).
/// </summary>
public sealed class LessonConfiguration
{
    public Dictionary<string, double> InitialState { get; set; } = [];

    public static LessonConfiguration FromState(IReadOnlyDictionary<string, double> state) =>
        new() { InitialState = new Dictionary<string, double>(state, StringComparer.OrdinalIgnoreCase) };
}
