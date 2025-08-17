namespace RestaurantSystem.Api.Features.Categories.Dtos;

public record CategoryProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsAvailable { get; init; }
    public bool IsPrimaryCategory { get; init; }
    public int PreparationTimeMinutes { get; init; }
}
