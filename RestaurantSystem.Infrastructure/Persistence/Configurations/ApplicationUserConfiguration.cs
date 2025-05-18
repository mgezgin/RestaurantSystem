using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RestaurantSystem.Domain.Common;
using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Infrastructure.Persistence.Configurations
{
    class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.ToTable("Users");

            builder.Property(u => u.Role)
            .HasConversion(new EnumToStringConverter<UserRole>());

            builder.HasIndex(u => u.Email)
                  .IsUnique();

            builder.HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            builder.HasIndex(u => u.NormalizedUserName)
                .IsUnique();

            builder.Property<Dictionary<string, string>>("Metadata")
              .HasColumnName("metadata")
              .HasColumnType("jsonb")
              .HasDefaultValueSql("'{}'::jsonb");

            builder.HasIndex(u => new { u.NormalizedUserName, u.IsDeleted })
              .IsUnique()
              .HasFilter("\"is_deleted\" = false");

            builder.HasIndex(u => new { u.NormalizedEmail, u.IsDeleted })
                .IsUnique()
                .HasFilter("\"is_deleted\" = false");
        }
    }
}
