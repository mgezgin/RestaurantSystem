using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Orders.Dtos;

public record CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public Guid? ProductVariationId { get; set; }
    public Guid? MenuId { get; set; }
    public int Quantity { get; set; }
    public string? SpecialInstructions { get; set; }
}
