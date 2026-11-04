using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimuLearn.Domain.Simulations;

public sealed record SceneValidationResult(SceneSpecification? Specification, IReadOnlyList<string> Errors)
{
    public bool IsValid => Specification is not null;

    public static SceneValidationResult Success(SceneSpecification specification) => new(specification, []);

    public static SceneValidationResult Failure(params string[] errors) => new(null, errors);
}

/// <summary>
/// Validates a scene specification coming from any generation provider. Unknown JSON members are
/// rejected outright, model keys must be allowlisted, and every parameter must exist in the model
/// and sit inside the server-defined range (FR-015, HAC-003).
/// </summary>
public sealed class SceneSpecificationValidator
{
    public const int MaxJsonLength = 16 * 1024;
    private const int MaxParameters = 32;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        MaxDepth = 12,
        NumberHandling = JsonNumberHandling.Strict,
    };

    private readonly SimulationModelRegistry _registry;

    public SceneSpecificationValidator(SimulationModelRegistry registry)
    {
        _registry = registry;
    }

    public SceneValidationResult ValidateJson(string? json, string source = "generated", string? provider = null)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return SceneValidationResult.Failure("The scene specification was empty.");
        }

        if (json.Length > MaxJsonLength)
        {
            return SceneValidationResult.Failure(
                $"The scene specification exceeded the {MaxJsonLength} byte limit.");
        }

        SceneSpecification? specification;
        try
        {
            specification = JsonSerializer.Deserialize<SceneSpecification>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return SceneValidationResult.Failure(
                $"The scene specification did not match the schema: {ex.Message}");
        }

        if (specification is null)
        {
            return SceneValidationResult.Failure("The scene specification was null.");
        }

        return Validate(specification, source, provider);
    }

    public SceneValidationResult Validate(
        SceneSpecification specification,
        string source = "generated",
        string? provider = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(specification.SimulationModel))
        {
            errors.Add("simulationModel is required.");
        }
        else if (!_registry.TryGet(specification.SimulationModel, out _))
        {
            errors.Add(
                $"Simulation model '{specification.SimulationModel}' is not supported. Supported models: " +
                string.Join(", ", _registry.All.Select(m => m.ModelKey)) + ".");
        }

        if (specification.InitialState.Count > MaxParameters)
        {
            errors.Add($"At most {MaxParameters} initial state parameters may be supplied.");
        }

        if (!_registry.TryGet(specification.SimulationModel, out var model))
        {
            return SceneValidationResult.Failure([.. errors]);
        }

        var state = new ParameterSet(specification.InitialState);
        var validation = model.Validate(state);
        foreach (var issue in validation.Issues)
        {
            errors.Add($"initialState.{issue.Parameter}: {issue.Message}");
        }

        var seenControls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var control in specification.Controls)
        {
            var parameter = model.Parameters.FirstOrDefault(
                p => string.Equals(p.Name, control.Parameter, StringComparison.OrdinalIgnoreCase));

            if (parameter is null)
            {
                errors.Add($"control '{control.Parameter}': the parameter is not supported by this model.");
                continue;
            }

            if (!seenControls.Add(parameter.Name))
            {
                errors.Add($"control '{parameter.Name}': the parameter is declared more than once.");
            }
        }

        if (errors.Count > 0)
        {
            return SceneValidationResult.Failure([.. errors]);
        }

        // The model, not the provider, decides labels, ranges, controls and safety copy.
        var canonical = SceneSpecificationFactory.Create(model, state, source, provider);
        return SceneValidationResult.Success(canonical);
    }
}
