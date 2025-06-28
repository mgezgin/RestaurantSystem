namespace RestaurantSystem.Api.Features.Products.Dtos;

public record ProductCategoryDto
{
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public int DisplayOrder { get; init; }
}
