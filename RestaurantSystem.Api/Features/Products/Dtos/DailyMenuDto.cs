namespace RestaurantSystem.Api.Features.Products.Dtos;

public class DailyMenuDto
{
    public Guid Id { get; set; }
    public DateOnly MenuDate { get; set; }
    public string? SpecialMessage { get; set; }
    public bool IsActive { get; set; }
    public List<DailyMenuProductDto> Products { get; set; } = new();
}
