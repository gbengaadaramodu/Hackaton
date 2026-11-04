using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SimuLearn.Application.Abstractions;
using SimuLearn.Domain.Common;
using SimuLearn.Domain.Identity;

namespace SimuLearn.Api.Infrastructure;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Builds the application actor from validated token claims. A caller with no subject claim is
    /// rejected rather than treated as an anonymous or default user.
    /// </summary>
    public static Actor ToActor(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(subject, out var userId))
        {
            throw new UnauthorizedException("The access token does not identify a user.");
        }

        var roles = principal
            .FindAll(ClaimTypes.Role)
            .Select(claim => Enum.TryParse<RoleName>(claim.Value, ignoreCase: true, out var role) ? role : (RoleName?)null)
            .Where(role => role is not null)
            .Select(role => role!.Value)
            .Distinct()
            .ToArray();

        return new Actor(
            userId,
            roles,
            principal.FindFirstValue(JwtRegisteredClaimNames.Email) ?? principal.FindFirstValue(ClaimTypes.Email));
    }

    public static Guid ActorId(this ClaimsPrincipal principal) => principal.ToActor().Id;
}
