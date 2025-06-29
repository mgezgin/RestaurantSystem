namespace RestaurantSystem.Api.Features.Basket.Dtos.Requests;

public record AddSideItemDto
{
    public Guid SideItemProductId { get; init; }
    public int Quantity { get; init; }
}