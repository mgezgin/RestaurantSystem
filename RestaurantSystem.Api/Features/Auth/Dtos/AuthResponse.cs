using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Auth.Dtos;

public record AuthResponse
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = null!;
    public string LastName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public UserRole Role { get; init; }
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
    public DateTime Expiration { get; init; }
}
