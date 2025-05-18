using Microsoft.AspNetCore.Authorization;
using RestaurantSystem.Domain.Common.Enums;
using System.Data;

namespace RestaurantSystem.Api.Common.Authorization
{
    /// <summary>
    /// Authorization attribute that requires the user to have one of the specified roles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RequireRoleAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireRoleAttribute"/> class.
        /// </summary>
        public RequireRoleAttribute(params UserRole[] roles)
        {
            if (roles == null || roles.Length == 0)
            {
                throw new ArgumentException("At least one role must be specified", nameof(roles));
            }

            Roles = string.Join(",", roles.Select(r => r.ToString()));
        }
    }

    /// <summary>
    /// Authorization attribute that requires the user to be an admin.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireAdminAttribute : RequireRoleAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireAdminAttribute"/> class.
        /// </summary>
        public RequireAdminAttribute() : base(UserRole.Admin)
        {
        }
    }

    /// <summary>
    /// Authorization attribute that requires the user to be a staff member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class RequireStaffAttribute : RequireRoleAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequireStaffAttribute"/> class.
        /// </summary>
        public RequireStaffAttribute()
            : base(UserRole.Admin, UserRole.Cashier, UserRole.KitchenStaff, UserRole.Server)
        {
        }
    }
}
