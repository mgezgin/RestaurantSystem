﻿using RestaurantSystem.Domain.Common.Base;

namespace RestaurantSystem.Domain.Entities;
public class BasketItem : Entity
{
    public Guid BasketId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? ProductVariationId { get; set; }
    public Guid? MenuId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemTotal { get; set; }
    public string? SpecialInstructions { get; set; }

    // Navigation properties
    public virtual Basket Basket { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual ProductVariation? ProductVariation { get; set; } = null;
    public virtual Menu? Menu { get; set; } = null!;
}
