namespace SimuLearn.Application.Abstractions;

public sealed record AiImageRequest(string Prompt);

public sealed record AiImageResponse(
    string? ImageUrl,
    string? ImageBase64,
    string Provider,
    string? Model);

/// <summary>Optional image-model boundary. SVG stays primary; callers keep the SVG fallback.</summary>
public interface IAiImageClient
{
    bool IsConfigured { get; }

    string ProviderName { get; }

    Task<AiImageResponse> GenerateAsync(AiImageRequest request, CancellationToken cancellationToken);
}
