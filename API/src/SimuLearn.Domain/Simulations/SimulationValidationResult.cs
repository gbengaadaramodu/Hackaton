namespace SimuLearn.Domain.Simulations;

/// <summary>A single rejected parameter, carrying a machine-readable code for the API contract.</summary>
public sealed record ParameterIssue(string Parameter, string Message, string Code = "invalid_value");

public sealed class SimulationValidationResult
{
    public static SimulationValidationResult Valid { get; } = new([]);

    public SimulationValidationResult(IReadOnlyList<ParameterIssue> issues)
    {
        Issues = issues;
    }

    public IReadOnlyList<ParameterIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public IReadOnlyDictionary<string, string[]> ToErrors()
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var issue in Issues)
        {
            var key = string.IsNullOrWhiteSpace(issue.Parameter) ? "state" : issue.Parameter;
            errors[key] = errors.TryGetValue(key, out var existing)
                ? [.. existing, issue.Message]
                : [issue.Message];
        }

        return errors;
    }

    public static SimulationValidationResult Invalid(params ParameterIssue[] issues) => new(issues);
}
