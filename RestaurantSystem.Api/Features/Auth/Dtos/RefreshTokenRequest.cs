using System.ComponentModel.DataAnnotations;

namespace RestaurantSystem.Api.Features.Auth.Dtos
{
    public class RefreshTokenRequest
    {
        [Required]
        public string AccessToken { get; init; } = null!;

        [Required]
        public string RefreshToken { get; init; } = null!;
    }
}
