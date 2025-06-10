using RestaurantSystem.Domain.Common.Base;

namespace RestaurantSystem.Domain.Entities;
public class DailyMenu : SoftDeleteEntity
{
    public DateOnly MenuDate { get; set; }
    public string? SpecialMessage { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<DailyMenuProduct> DailyMenuProducts { get; set; } = [];
}
