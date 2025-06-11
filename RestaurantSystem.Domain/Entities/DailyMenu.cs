using RestaurantSystem.Domain.Common.Base;

namespace RestaurantSystem.Domain.Entities;
public class DailyMenu : SoftDeleteEntity
{
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual ICollection<DailyMenuItem> MenuItems { get; set; } = [];
}
