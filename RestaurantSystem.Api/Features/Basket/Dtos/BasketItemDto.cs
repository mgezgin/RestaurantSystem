namespace RestaurantSystem.Api.Features.Basket.Dtos;

public record BasketItemDto
{
    public Guid Id { get; init; }
    public Guid? ProductId { get; init; }
    public string ProductName { get; init; } = null!;
    public string? ProductDescription { get; init; }
    public string? ProductImageUrl { get; init; }
    public Guid? ProductVariationId { get; init; }
    Guid? MenuId { get; init; }
    public string? VariationName { get; init; }
    public string? MenuName {  get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal ItemTotal { get; init; }
    public string? SpecialInstructions { get; init; }
}
