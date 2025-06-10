namespace RestaurantSystem.Api.Features.Products.Dtos;

public class SideItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
}
