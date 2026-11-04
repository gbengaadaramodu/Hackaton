namespace SimuLearn.Application.Abstractions;

public sealed record AiChatMessage(string Role, string Content);

public sealed record AiChatRequest(
    string SystemPrompt,
    string UserPrompt,
    double Temperature = 0.2,
    int MaxOutputTokens = 900,
    string? JsonSchemaName = null);

public sealed record AiChatResponse(string Content, string Provider, string? Model);

/// <summary>
/// Provider-neutral completion boundary. The platform must remain demonstrable when no model is
/// configured (BR-012, FR-016), so implementations report <see cref="IsConfigured"/> and callers
/// always keep a deterministic path.
/// </summary>
public interface IAiChatClient
{
    bool IsConfigured { get; }

    string ProviderName { get; }

    Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken);
}
