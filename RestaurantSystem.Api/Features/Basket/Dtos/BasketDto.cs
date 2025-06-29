namespace RestaurantSystem.Api.Features.Basket.Dtos;

public record BasketDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string SessionId { get; init; } = null!;
    public List<BasketItemDto> Items { get; init; } = new();
    public decimal SubTotal { get; init; }
    public decimal Tax { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Discount { get; init; }
    public decimal Total { get; init; }
    public string? PromoCode { get; init; }
    public int TotalItems { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? Notes { get; init; }
}
