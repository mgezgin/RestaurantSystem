using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Domain.Common;
using System.Security.Claims;

namespace RestaurantSystem.Api.Common.Authorization
{
    public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleAuthorizationHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (!context.User.Identity!.IsAuthenticated)
            {
                return; // Not authenticated, so fail
            }

            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return; // No user ID claim, so fail
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return; // User not found or deleted, so fail
            }

            // Check if user's role is in the list of allowed roles
            if (requirement.AllowedRoles.Contains(user.Role))
            {
                context.Succeed(requirement);
            }
        }

    }
}
