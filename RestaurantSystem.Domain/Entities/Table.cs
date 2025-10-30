using RestaurantSystem.Domain.Common.Base;

namespace RestaurantSystem.Domain.Entities;

public class Table : Entity
{
    public string TableNumber { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsOutdoor { get; set; } = false;

    // Position for visual layout (in pixels or percentage)
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public decimal Width { get; set; } = 80; // Default width
    public decimal Height { get; set; } = 80; // Default height

    // Navigation property
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}
