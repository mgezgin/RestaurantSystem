namespace RestaurantSystem.Api.Features.Products.Dtos;

public record DailyMenuDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public List<DailyMenuProductDto> Products { get; init; } = new();
}
