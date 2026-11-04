using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Common;

namespace SimuLearn.Infrastructure.Ai;

/// <summary>
/// Minimal chat-completions adapter. The API key is only ever read from configuration, is never
/// logged, and is never returned to a client (SEC-004, SRS 8.3).
/// </summary>
public sealed class OpenAiCompatibleChatClient : IAiChatClient, IDisposable
{
    private const int MaxResponseCharacters = 200_000;

    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiCompatibleChatClient> _logger;

    public OpenAiCompatibleChatClient(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OpenAiCompatibleChatClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        // Reasoning models (muse-wonder, o1, etc.) can take several minutes; allow up to 10 min.
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 600));

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            var rawUrl = _options.BaseUrl.Trim().TrimEnd('/');
            if (rawUrl.EndsWith("/responses", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = rawUrl[..^"/responses".Length];
            }
            else if (rawUrl.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
            {
                rawUrl = rawUrl[..^"/chat/completions".Length];
            }

            _httpClient.BaseAddress = new Uri(rawUrl.TrimEnd('/') + "/", UriKind.Absolute);
        }

        var apiKey = _options.EffectiveApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", apiKey);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool IsConfigured => _options.IsConfigured;

    public string ProviderName => string.IsNullOrWhiteSpace(_options.ProviderName)
        ? (_options.IsMeta ? "meta" : "openai-compatible")
        : _options.ProviderName;

    public async Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new GenerationFailedException("No language-model provider is configured.");
        }

        HttpResponseMessage response;
        if (_options.IsMeta)
        {
            var input = new List<object>();

            if (!string.IsNullOrWhiteSpace(request.UserPrompt))
            {
                input.Add(new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "input_text", text = request.UserPrompt }
                    }
                });
            }

            var metaPayload = new Dictionary<string, object?>
            {
                ["model"] = _options.Model,
                ["input"] = input,
                ["temperature"] = Math.Clamp(request.Temperature, 0d, 1.5d),
                ["max_output_tokens"] = Math.Max(request.MaxOutputTokens * 2, 8000),
                ["stream"] = false
            };

            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            {
                metaPayload["instructions"] = request.SystemPrompt;
            }

            response = await _httpClient.PostAsJsonAsync("responses", metaPayload, cancellationToken);
        }
        else
        {
            var payload = new Dictionary<string, object?>
            {
                ["model"] = _options.Model,
                ["temperature"] = Math.Clamp(request.Temperature, 0d, 1.5d),
                ["max_tokens"] = Math.Clamp(request.MaxOutputTokens, 50, 4000),
                ["messages"] = new object[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt },
                },
            };

            if (!string.IsNullOrWhiteSpace(request.JsonSchemaName))
            {
                payload["response_format"] = new { type = "json_object" };
            }

            response = await _httpClient.PostAsJsonAsync("chat/completions", payload, cancellationToken);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var excerpt = errorBody.Length > 500 ? errorBody[..500] : errorBody;

                _logger.LogWarning(
                    "AI provider {Provider} returned HTTP {StatusCode}. Body: {Body}",
                    ProviderName,
                    (int)response.StatusCode,
                    excerpt);

                throw new GenerationFailedException(
                    $"The language-model provider returned HTTP {(int)response.StatusCode}.");
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (body.Length > MaxResponseCharacters)
            {
                throw new GenerationFailedException("The language-model provider returned an oversized response.");
            }

            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                var content = ExtractContent(root);
                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new GenerationFailedException("The language-model provider returned an empty message.");
                }

                var model = root.TryGetProperty("model", out var modelElement) && modelElement.ValueKind == JsonValueKind.String
                    ? modelElement.GetString()
                    : _options.Model;

                return new AiChatResponse(content.Trim(), ProviderName, model);
            }
            catch (JsonException ex)
            {
                throw new GenerationFailedException("The language-model provider returned malformed JSON.", ex);
            }
        }
    }

    private static string? ExtractContent(JsonElement root)
    {
        // 1. Meta /responses structure: output[] -> message -> content[] -> text
        if (root.TryGetProperty("output", out var outputElement) && outputElement.ValueKind == JsonValueKind.Array)
        {
            var textParts = new List<string>();
            foreach (var item in outputElement.EnumerateArray())
            {
                var itemType = item.TryGetProperty("type", out var typeEl) ? typeEl.GetString() : null;
                var role = item.TryGetProperty("role", out var roleEl) ? roleEl.GetString() : null;

                // Skip pure reasoning traces unless they contain message text
                if (string.Equals(itemType, "reasoning", StringComparison.OrdinalIgnoreCase) && role == null)
                {
                    continue;
                }

                if (item.TryGetProperty("content", out var contentElement))
                {
                    if (contentElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var part in contentElement.EnumerateArray())
                        {
                            if (part.TryGetProperty("text", out var textEl) && textEl.ValueKind == JsonValueKind.String)
                            {
                                var text = textEl.GetString();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    textParts.Add(text);
                                }
                            }
                        }
                    }
                    else if (contentElement.ValueKind == JsonValueKind.String)
                    {
                        var text = contentElement.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            textParts.Add(text);
                        }
                    }
                }
            }

            if (textParts.Count > 0)
            {
                return string.Join("\n", textParts);
            }
        }

        // 2. Convenience top-level field output_text
        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString();
        }

        // 3. OpenAI standard: choices[0].message.content
        if (root.TryGetProperty("choices", out var choices)
            && choices.ValueKind == JsonValueKind.Array
            && choices.GetArrayLength() > 0)
        {
            var choice = choices[0];
            if (choice.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String)
            {
                return content.GetString();
            }
        }

        return null;
    }

    public void Dispose() => _httpClient.Dispose();
}
