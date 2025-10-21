namespace RestaurantSystem.Api.Features.Products.Dtos;

/// <summary>
/// DTO for product descriptions with language as key
/// </summary>
public record ProductDescriptionDto
{
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Ingredient { get; set; } = string.Empty!;
}
