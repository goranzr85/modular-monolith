using Microsoft.AspNetCore.Authorization;

namespace Modular.Authorization;

public sealed class PermissionAuthorizationRequirement(params string[] allowedPermissions)
    : AuthorizationHandler<PermissionAuthorizationRequirement>, IAuthorizationRequirement
{
    public string[] AllowedPermissions { get; } = allowedPermissions;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionAuthorizationRequirement requirement)
    {
        bool hasPermission = context.User.Claims.Any(c =>
        {
            if (c.Type != CustomClaimTypes.Permission)
                return false;

            if (requirement.AllowedPermissions.Contains(c.Value))
            {
                context.Succeed(requirement);
                return true;
            }
            return false;
        });

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
