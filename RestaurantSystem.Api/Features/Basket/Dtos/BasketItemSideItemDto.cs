namespace RestaurantSystem.Api.Features.Basket.Dtos;

public record BasketItemSideItemDto
{
    public Guid Id { get; init; }
    public Guid SideItemProductId { get; init; }
    public string SideItemName { get; init; } = null!;
    public string? SideItemDescription { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Total { get; init; }
}
