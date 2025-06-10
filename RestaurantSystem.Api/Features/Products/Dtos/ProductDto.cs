using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Products.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
    public int PreparationTimeMinutes { get; set; }
    public ProductType Type { get; set; }
    public List<string> Ingredients { get; set; } = new();
    public List<string> Allergens { get; set; } = new();
    public int DisplayOrder { get; set; }

    public List<ProductCategoryDto> Categories { get; set; } = new();
    public CategoryDto? PrimaryCategory { get; set; }
    public List<ProductVariationDto> Variations { get; set; } = new();
    public List<SideItemDto> SuggestedSideItems { get; set; } = new();
}

