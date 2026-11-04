using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Common;

namespace SimuLearn.Infrastructure.Ai;

/// <summary>
/// Optional OpenAI-compatible image adapter. The key is never logged or returned.
/// Prompts are built server-side from validated model facts only.
/// </summary>
public sealed class OpenAiCompatibleImageClient : IAiImageClient
{
    private const int MaxResponseCharacters = 12_000_000;

    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiCompatibleImageClient> _logger;

    public OpenAiCompatibleImageClient(
        HttpClient httpClient,
        IOptions<AiOptions> options,
        ILogger<OpenAiCompatibleImageClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

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

        if (!string.IsNullOrWhiteSpace(_options.EffectiveApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _options.EffectiveApiKey);
        }

        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public bool IsConfigured => _options.IsImageConfigured;

    public string ProviderName => string.IsNullOrWhiteSpace(_options.ProviderName)
        ? (_options.IsMeta ? "meta" : "openai-compatible")
        : _options.ProviderName;

    public async Task<AiImageResponse> GenerateAsync(AiImageRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            throw new GenerationFailedException("No image-model provider is configured.");
        }

        var endpoint = string.IsNullOrWhiteSpace(_options.ImageEndpoint)
            ? "images/generations"
            : _options.ImageEndpoint.Trim().TrimStart('/');

        var payload = new Dictionary<string, object?>
        {
            ["model"] = _options.ImageModel,
            ["prompt"] = request.Prompt,
            ["n"] = 1,
            ["size"] = "1024x1024",
        };

        using var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var excerpt = errorBody.Length > 500 ? errorBody[..500] : errorBody;

            _logger.LogWarning(
                "AI image provider {Provider} returned HTTP {StatusCode}. Body: {Body}",
                ProviderName,
                (int)response.StatusCode,
                excerpt);

            throw new GenerationFailedException(
                $"The image-model provider returned HTTP {(int)response.StatusCode}.");
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (body.Length > MaxResponseCharacters)
        {
            throw new GenerationFailedException("The image-model provider returned an oversized response.");
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            var (url, b64) = ExtractImage(document.RootElement);
            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(b64))
            {
                throw new GenerationFailedException("The image-model provider returned no image.");
            }

            var model = document.RootElement.TryGetProperty("model", out var modelElement)
                && modelElement.ValueKind == JsonValueKind.String
                    ? modelElement.GetString()
                    : _options.ImageModel;

            return new AiImageResponse(url, b64, ProviderName, model);
        }
        catch (JsonException ex)
        {
            throw new GenerationFailedException("The image-model provider returned malformed JSON.", ex);
        }
    }

    private static (string? Url, string? Base64) ExtractImage(JsonElement root)
    {
        if (root.TryGetProperty("data", out var data)
            && data.ValueKind == JsonValueKind.Array
            && data.GetArrayLength() > 0)
        {
            var first = data[0];
            var url = first.TryGetProperty("url", out var urlEl) && urlEl.ValueKind == JsonValueKind.String
                ? urlEl.GetString()
                : null;
            var b64 = first.TryGetProperty("b64_json", out var b64El) && b64El.ValueKind == JsonValueKind.String
                ? b64El.GetString()
                : null;
            return (url, b64);
        }

        if (root.TryGetProperty("image_url", out var top) && top.ValueKind == JsonValueKind.String)
        {
            return (top.GetString(), null);
        }

        return (null, null);
    }
}
