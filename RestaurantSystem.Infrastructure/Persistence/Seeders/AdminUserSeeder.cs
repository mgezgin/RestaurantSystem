using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Infrastructure.Persistence.Seeders;

public static class AdminUserSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        // Email of the user to grant admin role
        const string adminEmail = "admin2@email.com";
        const string adminRoleName = "Admin";

        // Ensure Admin role exists
        if (!await roleManager.RoleExistsAsync(adminRoleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(adminRoleName));
        }

        // Find the user by email
        var user = await userManager.FindByEmailAsync(adminEmail);
        if (user == null)
        {
            // User doesn't exist yet - they need to register first
            return;
        }

        // Check if user already has admin role (both Identity role and legacy enum)
        var hasIdentityRole = await userManager.IsInRoleAsync(user, adminRoleName);
        var hasLegacyRole = user.Role == UserRole.Admin;

        if (hasIdentityRole && hasLegacyRole)
        {
            // User already has admin role in both systems
            return;
        }

        // Assign Identity role if not present
        if (!hasIdentityRole)
        {
            await userManager.AddToRoleAsync(user, adminRoleName);
        }

        // Update legacy Role enum field if not admin
        if (!hasLegacyRole)
        {
            user.Role = UserRole.Admin;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = "System";
            await userManager.UpdateAsync(user);
        }

        await context.SaveChangesAsync();
    }
}
