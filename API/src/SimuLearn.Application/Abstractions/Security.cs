using SimuLearn.Domain.Identity;

namespace SimuLearn.Application.Abstractions;

public interface IPasswordHasher
{
    /// <summary>Hashes a password with a per-user salt. The returned string embeds algorithm parameters.</summary>
    string Hash(string password);

    bool Verify(string password, string storedHash);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);

public interface ITokenService
{
    AccessToken CreateAccessToken(User user, DateTimeOffset issuedAt);
}
