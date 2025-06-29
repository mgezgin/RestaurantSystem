namespace RestaurantSystem.Api.Features.Basket.Dtos.Requests;

public record AddToBasketDto
{
    public Guid ProductId { get; init; }
    public Guid? ProductVariationId { get; init; }
    public int Quantity { get; init; }
    public string? SpecialInstructions { get; init; }
    public List<AddSideItemDto>? SideItems { get; init; }
}

