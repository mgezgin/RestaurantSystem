using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Common;

namespace RestaurantSystem.Api.Features.Users.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetByIdAsync(Guid id);
        Task<List<ApplicationUser>> GetAllAsync();
        Task<ApplicationUser> UpdateUserAsync(Guid id, UpdateUserRequest request);
        Task<bool> DeleteUserAsync(Guid id, string deletedBy);
        Task<bool> ChangeUserRoleAsync(Guid id, UserRole newRole);
        Task<bool> ResetPasswordAsync(Guid id, string newPassword);
    }
}
