using RestaurantSystem.Domain.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace RestaurantSystem.Api.Features.Auth.Dtos;

public record RegisterRequest
{
    [Required]
    public string FirstName { get; init; } = null!;

    [Required]
    public string LastName { get; init; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = null!;

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; init; } = null!;

    public UserRole Role { get; init; } = UserRole.Customer;
}
