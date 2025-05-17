using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSystem.Domain.Identity;

namespace RestaurantSystem.Infrastructure.Persistence.Configurations
{
    class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users");

            builder.Property(e => e.FirstName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.LastName)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(e => e.CreatedAt)
                .IsRequired();

            builder.Property(e => e.RefreshToken)
                .HasMaxLength(256);
        }
    }
}
