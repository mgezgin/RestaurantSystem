using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Domain.Common.Interfaces;

namespace RestaurantSystem.Domain.Identity
{
    public class ApplicationUser : IdentityUser<Guid>, IAuditable, ISoftDelete
    {



        public required string FirstName { get; set; }
        public required string LastName { get; set; }


        // Audit properties
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public required string CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }

        // Soft delete properties
        public bool IsDeleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedBy { get; set; }

        public required string RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}
