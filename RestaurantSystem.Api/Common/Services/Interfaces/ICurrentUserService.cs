using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Common.Services.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
        string? UserName { get; }
        string? Email { get; }
        UserRole? Role { get; }
        bool IsAuthenticated { get; }
        bool IsAdmin { get; }
        Task<ApplicationUser?> GetUserAsync();
    }
}
