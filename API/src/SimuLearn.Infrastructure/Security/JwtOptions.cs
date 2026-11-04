namespace SimuLearn.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "simulearn";

    public string Audience { get; set; } = "simulearn-client";

    /// <summary>HMAC signing key. Supplied through configuration or a secret store, never committed.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenMinutes { get; set; } = 120;
}
