using System.ComponentModel.DataAnnotations;

namespace RestaurantSystem.Api.Features.Auth.Dtos;

public class LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    public string Password { get; init; } = null!;
}
