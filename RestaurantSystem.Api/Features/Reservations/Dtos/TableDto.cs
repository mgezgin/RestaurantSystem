namespace RestaurantSystem.Api.Features.Reservations.Dtos;

public record TableDto
{
    public Guid Id { get; set; }
    public string TableNumber { get; set; } = string.Empty;
    public int MaxGuests { get; set; }
    public bool IsActive { get; set; }
    public bool IsOutdoor { get; set; }
    public decimal PositionX { get; set; }
    public decimal PositionY { get; set; }
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public string Shape { get; set; } = "circle";
    public int Rotation { get; set; } = 0;
    public string? Notes { get; set; }
    public string? QRCodeData { get; set; }
    public DateTime? QRCodeGeneratedAt { get; set; }
}
