using System.Text.Json;
using System.Text.Json.Serialization;
using SimuLearn.Domain.Simulations;

namespace SimuLearn.Application.Common;

public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, Options);
}

public static class SessionStateSerializer
{
    public static Dictionary<string, double> Deserialize(string? json) =>
        JsonDefaults.Deserialize<Dictionary<string, double>>(json)
        ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

    public static string Serialize(ParameterSet state) => JsonDefaults.Serialize(state.ToDictionary());
}
