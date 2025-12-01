using Microsoft.AspNetCore.Authorization;

namespace Modular.Authorization;

public static class PermissionExtensions
{
    public static void RequirePermission(
        this AuthorizationPolicyBuilder builder,
        params string[] allowedPermissions)
    {
        builder.AddRequirements(new PermissionAuthorizationRequirement(allowedPermissions));
    }
}
