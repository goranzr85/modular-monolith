using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Modular.Authorization;

public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    private const string _resourceAccessClaimName = "resource_access";
    private readonly string _clientId = "eshop-public";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        ClaimsIdentity identity = (ClaimsIdentity)principal.Identity!;

        Claim? resourceAccessClaim = identity.FindFirst(_resourceAccessClaimName);
        if (resourceAccessClaim is null)
        {
            return Task.FromResult(principal);
        }

        using JsonDocument doc = JsonDocument.Parse(resourceAccessClaim.Value);

        if (doc.RootElement.TryGetProperty(_clientId, out var clientAccess) &&
            clientAccess.TryGetProperty("roles", out var roles))
        {
            foreach (var role in roles.EnumerateArray())
            {
                string? roleValue = role.GetString();
                if (!string.IsNullOrEmpty(roleValue))
                {
                    identity.AddClaim(new Claim(CustomClaimTypes.Permission, roleValue));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
