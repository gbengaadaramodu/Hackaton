using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

namespace SimuLearn.Api.Infrastructure;

/// <summary>One serializer configuration shared by HTTP responses, ProblemDetails and idempotency replay.</summary>
public static class ApiJson
{
    public static readonly JsonSerializerOptions Options = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = null;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static void ConfigureHttpJsonOptions(JsonOptions options) => Configure(options.SerializerOptions);
}
