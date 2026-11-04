namespace SimuLearn.Infrastructure.Ai;

/// <summary>
/// Provider completion settings supporting Meta AI (/v1/responses) and OpenAI-compatible
/// (/v1/chat/completions) endpoints. Without a configured key the platform runs entirely
/// on deterministic fallbacks.
/// </summary>
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public bool Enabled { get; set; } = true;

    public string BaseUrl { get; set; } = "https://api.meta.ai/v1";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "muse-spark-1.3";

    public string ProviderName { get; set; } = "meta";

    /// <summary>
    /// Per-call HTTP timeout in seconds. Reasoning models (e.g. muse-wonder) need 120-180 s.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>Optional image generation. Off by default; SVG remains the primary renderer.</summary>
    public bool ImageEnabled { get; set; }

    public string ImageModel { get; set; } = string.Empty;

    public string ImageEndpoint { get; set; } = "images/generations";

    public string EffectiveApiKey =>
        !string.IsNullOrWhiteSpace(ApiKey)
            ? ApiKey
            : Environment.GetEnvironmentVariable("MODEL_API_KEY") ?? string.Empty;

    public bool IsMeta =>
        string.Equals(ProviderName, "meta", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ProviderName, "meta-ai", StringComparison.OrdinalIgnoreCase)
        || (!string.IsNullOrWhiteSpace(BaseUrl) && BaseUrl.Contains("api.meta.ai", StringComparison.OrdinalIgnoreCase));

    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(EffectiveApiKey)
        && !string.IsNullOrWhiteSpace(Model);

    public bool IsImageConfigured =>
        Enabled
        && ImageEnabled
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(EffectiveApiKey)
        && !string.IsNullOrWhiteSpace(ImageModel);
}
