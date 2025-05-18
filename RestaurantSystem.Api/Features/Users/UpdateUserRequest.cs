using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Users
{
    public class UpdateUserRequest
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string? Email { get; set; }
        public UserRole? Role { get; set; }
    }
}
