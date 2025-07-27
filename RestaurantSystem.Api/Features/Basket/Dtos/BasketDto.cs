﻿namespace RestaurantSystem.Api.Features.Basket.Dtos;

public record BasketDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string SessionId { get; set; } = null!;
    public decimal SubTotal { get; set; }
    public decimal Tax { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Discount { get; set; }
    public decimal Total { get; set; }
    public string? PromoCode { get; set; }
    public int TotalItems { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Notes { get; set; }
    public List<BasketItemDto> Items { get; set; } = new();
}
