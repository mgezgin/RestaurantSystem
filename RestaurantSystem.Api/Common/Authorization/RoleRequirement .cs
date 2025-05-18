using Microsoft.AspNetCore.Authorization;
using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Common.Authorization
{
    /// <summary>
    /// Authorization requirement for role-based access control.
    /// </summary>
    public class RoleRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Gets the roles that are allowed to access the resource.
        /// </summary>
        public UserRole[] AllowedRoles { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleRequirement"/> class.
        /// </summary>
        public RoleRequirement(UserRole[] allowedRoles)
        {
            AllowedRoles = allowedRoles ?? throw new ArgumentNullException(nameof(allowedRoles));
        }
    }
}
