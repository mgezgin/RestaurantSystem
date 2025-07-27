namespace RestaurantSystem.Api.Features.Basket.Dtos.Requests;

public record AddToBasketDto
{
    public Guid ProductId { get; set; } = Guid.Empty;
    public Guid? ProductVariationId { get; set; }
    public Guid? MenuId { get; set; }
    public int Quantity { get; set; } = 1;
    public string? SpecialInstructions { get; set; }
}

