using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Products.Dtos;

public class ProductSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
    public ProductType Type { get; set; }
    public List<string> CategoryNames { get; set; } = new();
    public string? PrimaryCategoryName { get; set; }
    public int VariationCount { get; set; }
    public int SideItemCount { get; set; }
}