namespace SimuLearn.Domain.Common;

/// <summary>
/// Base class for expected business failures. The API layer maps <see cref="Code"/> to an
/// HTTP status and a machine-readable ProblemDetails code; nothing here depends on HTTP.
/// </summary>
public abstract class DomainException(string code, string message, Exception? inner = null)
    : Exception(message, inner)
{
    public string Code { get; } = code;
}

public sealed class NotFoundException(string resource, object id)
    : DomainException("not_found", $"{resource} '{id}' was not found.");

public sealed class UnauthorizedException(string message = "Sign in to continue.")
    : DomainException("unauthorized", message);

public sealed class ForbiddenException(string message = "You are not allowed to perform this action.")
    : DomainException("forbidden", message);

public sealed class ConflictException(string message, string code = "conflict")
    : DomainException(code, message);

public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message, IReadOnlyDictionary<string, string[]>? errors = null)
        : base("validation_failed", message)
    {
        Errors = errors ?? new Dictionary<string, string[]>();
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}

public sealed class UnsupportedRequestException(string message, IReadOnlyList<string>? clarifyingQuestions = null)
    : DomainException("unsupported_request", message)
{
    public IReadOnlyList<string> ClarifyingQuestions { get; } = clarifyingQuestions ?? [];
}

public sealed class GenerationFailedException(string message, Exception? inner = null)
    : DomainException("generation_failed", message, inner);
