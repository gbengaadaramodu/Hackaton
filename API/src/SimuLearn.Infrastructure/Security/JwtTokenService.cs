using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Identity;

namespace SimuLearn.Infrastructure.Security;

/// <summary>Issues short-lived HMAC-signed access tokens carrying the user's roles.</summary>
public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AccessToken CreateAccessToken(User user, DateTimeOffset issuedAt)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.SigningKey) || settings.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured with at least 32 characters before tokens can be issued.");
        }

        var expiresAt = issuedAt.AddMinutes(Math.Clamp(settings.AccessTokenMinutes, 5, 1440));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
        };

        foreach (var role in user.Roles.Select(r => r.Role).Distinct())
        {
            claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.SigningKey) || settings.SigningKey.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey must be configured with at least 32 characters before the API can start.");
        }

        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
    }

}
