namespace RestaurantSystem.Api.Features.Basket.Dtos.Requests;

public record SelectedMenuOptionDto
{
    public Guid SectionId { get; set; }
    public Guid ItemId { get; set; }
    public int Quantity { get; set; } = 1;
}
