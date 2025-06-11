using RestaurantSystem.Domain.Common.Base;

namespace RestaurantSystem.Domain.Entities;
public class DailyMenuItem : Entity
{
    public bool IsAvailable { get; set; } = true;
    public decimal? SpecialPrice { get; set; } // Override regular price if set
    public int? EstimatedQuantity { get; set; }
    public int DisplayOrder { get; set; }

    // Foreign Keys
    public Guid DailyMenuId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation properties
    public virtual DailyMenu DailyMenu { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
