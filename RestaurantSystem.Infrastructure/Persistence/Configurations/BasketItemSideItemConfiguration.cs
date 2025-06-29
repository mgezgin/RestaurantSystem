using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Infrastructure.Persistence.Configurations;
public class BasketItemSideItemConfiguration : IEntityTypeConfiguration<BasketItemSideItem>
{
    public void Configure(EntityTypeBuilder<BasketItemSideItem> builder)
    {
        builder.ToTable("BasketItemSideItems");

        builder.Property(si => si.Quantity)
            .IsRequired();

        builder.Property(si => si.UnitPrice)
            .HasColumnType("decimal(10,2)");

        builder.HasIndex(si => si.BasketItemId);

        // Relationship with side item product
        builder.HasOne(si => si.SideItemProduct)
            .WithMany()
            .HasForeignKey(si => si.SideItemProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}