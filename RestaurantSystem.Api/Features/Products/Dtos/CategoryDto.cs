namespace RestaurantSystem.Api.Features.Products.Dtos;

public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public int ProductCount { get; set; }
    public int PrimaryProductCount { get; set; } // Count of products where this is primary category
}
