using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Users.Interfaces;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Common;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Common.Exceptions;

namespace RestaurantSystem.Api.Features.Users;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentUserService _currentUserService;

    public UserService(
        UserManager<ApplicationUser> userManager,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _currentUserService = currentUserService;
    }

    public async Task<ApplicationUser> GetByIdAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null || user.IsDeleted)
        {
            throw new KeyNotFoundException($"User with ID {id} not found");
        }

        return user;
    }

    public async Task<List<ApplicationUser>> GetAllAsync()
    {
        // Only return non-deleted users
        return await _userManager.Users
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync();
    }

    public async Task<ApplicationUser> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await GetByIdAsync(id);

        // Check if the current user has permission to update this user
        var currentUser = await _currentUserService.GetUserAsync();
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException();
        }

        // Check if user is updating themselves or is an admin
        if (currentUser.Id != user.Id && currentUser.Role != UserRole.Admin)
        {
            throw new ForbiddenException("You don't have permission to update this user");
        }

        // Update user properties
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;

        // Only admin can change email and role
        if (currentUser.Role == UserRole.Admin)
        {
            if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
            {
                user.Email = request.Email;
                user.UserName = request.Email;
            }

            if (request.Role.HasValue && request.Role != user.Role)
            {
                // Remove from old role
                await _userManager.RemoveFromRoleAsync(user, user.Role.ToString());

                // Add to new role
                user.Role = request.Role.Value;
                await _userManager.AddToRoleAsync(user, request.Role.Value.ToString());
            }
        }

        // Update audit fields
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser.Id.ToString();

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to update user: {errors}");
        }

        return user;
    }

    public async Task<bool> DeleteUserAsync(Guid id, string deletedBy)
    {
        var user = await GetByIdAsync(id);

        // Soft delete
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.DeletedBy = deletedBy;

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task<bool> ChangeUserRoleAsync(Guid id, UserRole newRole)
    {
        var user = await GetByIdAsync(id);
        var currentUser = await _currentUserService.GetUserAsync();

        if (currentUser == null || currentUser.Role != UserRole.Admin)
        {
            throw new ForbiddenException("Only administrators can change user roles");
        }

        // Remove from old role
        await _userManager.RemoveFromRoleAsync(user, user.Role.ToString());

        // Add to new role
        user.Role = newRole;
        await _userManager.AddToRoleAsync(user, newRole.ToString());

        // Update user
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser.Id.ToString();

        var result = await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }

    public async Task<bool> ResetPasswordAsync(Guid id, string newPassword)
    {
        var user = await GetByIdAsync(id);
        var currentUser = await _currentUserService.GetUserAsync();

        if (currentUser == null)
        {
            throw new UnauthorizedAccessException();
        }

        // Check if user is updating themselves or is an admin
        if (currentUser.Id != user.Id && currentUser.Role != UserRole.Admin)
        {
            throw new ForbiddenException("You don't have permission to reset this user's password");
        }

        // Reset password
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to reset password: {errors}");
        }

        // Update user
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser.Id.ToString();
        await _userManager.UpdateAsync(user);

        return result.Succeeded;
    }
}
