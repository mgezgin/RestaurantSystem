using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Infrastructure.Persistence.Configurations;
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {

        builder.ToTable("Products");

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.BasePrice)
            .HasColumnType("decimal(10,2)");

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(p => p.Ingredients)
            .HasColumnType("jsonb");

        builder.Property(p => p.Allergens)
            .HasColumnType("jsonb");

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(p => p.PreparationTimeMinutes)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasConversion<int>(); // Store enum as int, or use EnumToStringConverter if needed

        builder.HasIndex(p => p.DisplayOrder);

        // Configure navigation: Suggested side items (Main -> Side)
        builder.HasMany(p => p.SuggestedSideItems)
            .WithOne(si => si.MainProduct)
            .HasForeignKey(si => si.MainProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure navigation: Product is used as a side item (Side -> Main)
        builder.HasMany(p => p.SideItemProducts)
            .WithOne(si => si.SideItemProduct)
            .HasForeignKey(si => si.SideItemProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure other relationships (optional, if needed)
        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey("ProductId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.ProductCategories)
            .WithOne()
            .HasForeignKey("ProductId");

        builder.HasMany(p => p.Variations)
            .WithOne()
            .HasForeignKey("ProductId");

        builder.HasMany(p => p.MenuProducts)
            .WithOne()
            .HasForeignKey("ProductId");

    }
}
