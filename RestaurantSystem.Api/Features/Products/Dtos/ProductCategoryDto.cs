namespace RestaurantSystem.Api.Features.Products.Dtos;

public class ProductCategoryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public int DisplayOrder { get; set; }
}
