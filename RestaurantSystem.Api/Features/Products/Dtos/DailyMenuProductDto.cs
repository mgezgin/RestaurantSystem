namespace RestaurantSystem.Api.Features.Products.Dtos;

public class DailyMenuProductDto
{
    public Guid Id { get; set; }
    public ProductSummaryDto Product { get; set; } = null!;
    public bool IsAvailable { get; set; }
    public decimal? SpecialPrice { get; set; }
    public decimal FinalPrice { get; set; } // SpecialPrice ?? Product.BasePrice
    public int? EstimatedQuantity { get; set; }
    public int DisplayOrder { get; set; }
}
