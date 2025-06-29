namespace RestaurantSystem.Api.Features.Basket.Dtos;

public record BasketSummaryDto
{
    public Guid Id { get; init; }
    public int ItemCount { get; init; }
    public decimal Total { get; init; }
}
