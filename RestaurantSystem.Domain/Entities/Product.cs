﻿using RestaurantSystem.Domain.Common.Base;
using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Domain.Entities;
public class Product : SoftDeleteEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public string? ImageUrl { get; init; } // Primary image URL for backward compatibility
    public bool IsActive { get; set; } = true;
    public bool IsAvailable { get; set; } = true;
    public bool IsSpecial { get; set; } = false; // Is this a special menu (e.g., holiday menu)
    public int PreparationTimeMinutes { get; set; }
    public ProductType Type { get; set; } = ProductType.MainItem;
    public List<string>? Ingredients { get; set; } // JSON array of ingredients
    public List<string>? Allergens { get; set; } // JSON array of allergens
    public int DisplayOrder { get; set; }

    // Navigation properties
    public virtual ICollection<ProductImage> Images { get; set; } = [];
    public virtual ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();
    public virtual ICollection<ProductVariation> Variations { get; set; } = [];
    public virtual ICollection<ProductSideItem> SuggestedSideItems { get; set; } = [];
    public virtual ICollection<ProductSideItem> SideItemProducts { get; set; } = [];
    public virtual ICollection<MenuItem> MenuProducts { get; set; } = [];
    public virtual ICollection<ProductDescription> Descriptions { get; set; } = [];

}
