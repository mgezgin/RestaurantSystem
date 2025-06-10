namespace RestaurantSystem.Api.Features.Products.Dtos;

public class ProductVariationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal PriceModifier { get; set; }
    public decimal FinalPrice { get; set; } // BasePrice + PriceModifier
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
}