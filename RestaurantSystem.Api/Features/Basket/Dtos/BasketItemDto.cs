namespace RestaurantSystem.Api.Features.Basket.Dtos;

public record BasketItemDto
{
    // Product details
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public string? ProductImageUrl { get; set; }
    public Guid? ProductVariationId { get; set; }
    public string? VariationName { get; set; }

    // Menu details
    public Guid? MenuId { get; set; }
    public string? MenuName { get; set; }
    public DateOnly? MenuDate { get; set; }
    public List<MenuItemSummaryDto>? MenuItems { get; set; }

    // Common properties
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemTotal { get; set; }
    public string? SpecialInstructions { get; set; }
}
