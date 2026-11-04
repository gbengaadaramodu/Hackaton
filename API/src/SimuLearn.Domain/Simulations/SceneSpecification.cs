using System.Text.Json.Serialization;

namespace SimuLearn.Domain.Simulations;

/// <summary>
/// A structured, versioned description of a learning scene. This is the only artefact a generation
/// provider may return; the browser maps it to trusted renderers and the server maps it to an
/// allowlisted simulation model (SRS 4.4).
/// </summary>
public sealed class SceneSpecification
{
    public string SceneId { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string LearningObjective { get; set; } = string.Empty;

    public string LearningObjectiveCode { get; set; } = string.Empty;

    public string Renderer { get; set; } = "svg-2d";

    /// <summary>Allowlisted simulation model key; never a script or URL.</summary>
    public string SimulationModel { get; set; } = string.Empty;

    public string SchemaVersion { get; set; } = string.Empty;

    public Dictionary<string, double> InitialState { get; set; } = [];

    public List<SceneControl> Controls { get; set; } = [];

    public List<string> Features { get; set; } = [];

    public List<string> Assumptions { get; set; } = [];

    public List<string> Limitations { get; set; } = [];

    /// <summary>"template" or "generated".</summary>
    public string Source { get; set; } = "template";

    public string? Provider { get; set; }

    [JsonIgnore]
    public bool IsGenerated => string.Equals(Source, "generated", StringComparison.OrdinalIgnoreCase);
}

public sealed record SceneControl(
    string Parameter,
    string Label,
    double Min,
    double Max,
    double Step,
    string Unit,
    double Default,
    string? Symbol = null);

public static class SceneSpecificationFactory
{
    public static SceneSpecification Create(
        ISimulationModel model,
        ParameterSet? initialState = null,
        string source = "template",
        string? provider = null)
    {
        var state = model.Normalize(initialState ?? model.CreateInitialState());

        return new SceneSpecification
        {
            SceneId = $"{model.ModelKey}-scene",
            Subject = model.Subject,
            LearningObjective = model.LearningObjective,
            LearningObjectiveCode = model.LearningObjectiveCode,
            Renderer = model.Renderer,
            SimulationModel = model.ModelKey,
            SchemaVersion = model.SchemaVersion,
            InitialState = state.ToDictionary(),
            Controls =
            [
                .. model.Parameters.Select(p => new SceneControl(
                    p.Name, p.Label, p.Min, p.Max, p.Step, p.Unit, p.Default, p.Symbol)),
            ],
            Features = [.. model.Features],
            Assumptions = [.. model.Assumptions],
            Limitations = [.. model.Limitations],
            Source = source,
            Provider = provider,
        };
    }
}
