namespace RestaurantSystem.Api.Features.Orders.Dtos;

public record OrderItemDto
{
    public Guid Id { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ProductVariationId { get; set; }
    public Guid? MenuID { get; set; }
    public string ProductName { get; set; } = null!;
    public string? VariationName { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemTotal { get; set; }
    public string? SpecialInstructions { get; set; }
}
