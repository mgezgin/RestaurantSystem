namespace RestaurantSystem.Api.Features.Products.Dtos;

public record DailyMenuProductDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string? ProductDescription { get; init; }
    public decimal BasePrice { get; init; }
    public string? ProductImageUrl { get; init; }
    public Guid? ProductVariationId { get; init; }
    public string? VariationName { get; init; }
    public decimal? VariationPriceModifier { get; init; }
    public decimal? SpecialPrice { get; init; }
    public decimal FinalPrice { get; init; }
    public int? MaxQuantity { get; init; }
    public string? Notes { get; init; }
    public int DisplayOrder { get; init; }
}
