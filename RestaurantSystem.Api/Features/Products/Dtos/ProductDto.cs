﻿using RestaurantSystem.Api.Features.Categories.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Features.Products.Dtos;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal BasePrice { get; init; }
    public string? ImageUrl { get; init; } // Primary image URL for backward compatibility
    public bool IsActive { get; init; }
    public bool IsAvailable { get; init; }

    public bool IsSpecial { get; init; } // Is this a special menu (e.g., holiday menu)
    public int PreparationTimeMinutes { get; init; }
    public ProductType Type { get; init; }
    public List<string>? Ingredients { get; init; } = [];
    public List<string>? Allergens { get; init; } = [];
    public int DisplayOrder { get; init; }
    public ProductDescriptionsDto Content { get; set; } = new();

    public List<ProductImageDto> Images { get; init; } = [];
    public List<ProductCategoryDto> Categories { get; init; } = [];
    public CategoryDto? PrimaryCategory { get; init; }
    public List<ProductVariationDto> Variations { get; init; } = [];
    public List<SideItemDto> SuggestedSideItems { get; init; } = [];

}

