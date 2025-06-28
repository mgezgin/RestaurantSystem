namespace RestaurantSystem.Api.Features.Products.Dtos;

public record CategoryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public int? ProductCount { get; init; }
}
