using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Products.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Products.Commands.CreateProductCommand;

public record CreateProductCommand(
    string Name,
    string? Description,
    decimal BasePrice,
    string? ImageUrl,
    bool IsActive,
    bool IsAvailable,
    int PreparationTimeMinutes,
    ProductType Type,
    List<string> Ingredients,
    List<string> Allergens,
    int DisplayOrder,
    List<Guid> CategoryIds,
    Guid? PrimaryCategoryId,
    List<CreateProductVariationDto>? Variations,
    List<Guid>? SuggestedSideItemIds,
    ProductDescriptionsDto Content
) : ICommand<ApiResponse<ProductDto>>;

public record CreateProductVariationDto(
    string Name,
    string? Description,
    decimal PriceModifier,
    bool IsActive,
    int DisplayOrder
);

public class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ApiResponse<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProductCommandHandler> _logger;

    public CreateProductCommandHandler(ApplicationDbContext context, ICurrentUserService currentUserService, ILogger<CreateProductCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<ProductDto>> Handle(CreateProductCommand command, CancellationToken cancellationToken)
    {

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var categories = await _context.Categories
       .Where(c => command.CategoryIds.Contains(c.Id))
       .ToListAsync(cancellationToken);

            // Validate primary category
            if (command.PrimaryCategoryId.HasValue && !command.CategoryIds.Contains(command.PrimaryCategoryId.Value))
            {
                return ApiResponse<ProductDto>.Failure("Primary category must be one of the selected categories");
            }

            if (command.SuggestedSideItemIds?.Any() == true)
            {
                var sideItemsExist = await _context.Products
                    .Where(p => command.SuggestedSideItemIds.Contains(p.Id) && p.Type == ProductType.SideItem)
                    .CountAsync(cancellationToken) == command.SuggestedSideItemIds.Count;

                if (!sideItemsExist)
                {
                    return ApiResponse<ProductDto>.Failure("One or more suggested side items not found or not side items");
                }
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = command.Name,
                Description = command.Description,
                BasePrice = command.BasePrice,
                IsActive = command.IsActive,
                IsAvailable = command.IsAvailable,
                PreparationTimeMinutes = command.PreparationTimeMinutes,
                Type = command.Type,
                Ingredients = command.Ingredients.Any()
                  ? System.Text.Json.JsonSerializer.Serialize(command.Ingredients)
                  : null,
                Allergens = command.Allergens.Any()
                  ? System.Text.Json.JsonSerializer.Serialize(command.Allergens)
                  : null,
                DisplayOrder = command.DisplayOrder,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
            };

            _context.Products.Add(product);

            var displayOrder = 0;

            foreach (var categoryId in command.CategoryIds)
            {
                var productCategory = new ProductCategory
                {
                    CategoryId = categoryId,
                    IsPrimary = categoryId == command.PrimaryCategoryId,
                    DisplayOrder = displayOrder++,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };
                _context.ProductCategories.Add(productCategory);
                product.ProductCategories.Add(productCategory);
            }

            foreach (var (languageCode, description) in command.Content)
            {

                var isAny = await _context.ProductDescriptions.AnyAsync(x => string.Equals(languageCode, x.Lang));

                if (isAny)
                {
                    return ApiResponse<ProductDto>.Failure($"language {languageCode} used more than one");
                }

                var productDescription = new ProductDescription
                {
                    Lang = languageCode,
                    Name = description.Name,
                    Description = description.Description,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };
                _context.ProductDescriptions.Add(productDescription);
                product.Descriptions.Add(productDescription);
            }

            if (command.Variations?.Any() == true)
            {
                foreach (var variationDto in command.Variations)
                {
                    var variation = new ProductVariation
                    {
                        Name = variationDto.Name,
                        Description = variationDto.Description,
                        PriceModifier = variationDto.PriceModifier,
                        IsActive = variationDto.IsActive,
                        DisplayOrder = variationDto.DisplayOrder,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                    };
                    _context.ProductVariations.Add(variation);
                    product.Variations.Add(variation);
                }
            }

            if (command.SuggestedSideItemIds?.Any() == true)
            {
                var sideItemDisplayOrder = 0;
                foreach (var sideItemId in command.SuggestedSideItemIds)
                {
                    var productSideItem = new ProductSideItem
                    {
                        MainProductId = product.Id,
                        SideItemProductId = sideItemId,
                        IsRequired = false,
                        DisplayOrder = sideItemDisplayOrder++,
                        CreatedAt = DateTime.UtcNow,
                        CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                    };
                    _context.ProductSideItems.Add(productSideItem);
                    product.SuggestedSideItems.Add(productSideItem);
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            var createdProduct = await _context.Products
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.Variations)
                .Include(p => p.SuggestedSideItems)
                    .ThenInclude(si => si.SideItemProduct)
                .FirstAsync(p => p.Id == product.Id, cancellationToken);

            var productDto = MapToProductDto(createdProduct);

            _logger.LogInformation("Product {ProductId} created successfully by user {UserId}",
                    product.Id, _currentUserService.UserId);

            return ApiResponse<ProductDto>.SuccessWithData(productDto, "Product created successfully");

        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

       
    }

    private static ProductDto MapToProductDto(Product product)
    {
        var dto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            IsActive = product.IsActive,
            IsAvailable = product.IsAvailable,
            PreparationTimeMinutes = product.PreparationTimeMinutes,
            Type = product.Type,
            Ingredients = string.IsNullOrEmpty(product.Ingredients)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.Ingredients) ?? new List<string>(),
            Allergens = string.IsNullOrEmpty(product.Allergens)
                ? new List<string>()
                : System.Text.Json.JsonSerializer.Deserialize<List<string>>(product.Allergens) ?? new List<string>(),
            DisplayOrder = product.DisplayOrder,
            Categories = product.ProductCategories.Select(pc => new ProductCategoryDto
            {
                CategoryId = pc.CategoryId,
                CategoryName = pc.Category.Name,
                IsPrimary = pc.IsPrimary,
                DisplayOrder = pc.DisplayOrder
            }).ToList(),
            PrimaryCategory = product.ProductCategories
                .Where(pc => pc.IsPrimary)
                .Select(pc => new CategoryDto
                {
                    Id = pc.Category.Id,
                    Name = pc.Category.Name,
                    Description = pc.Category.Description,
                    ImageUrl = pc.Category.ImageUrl,
                    IsActive = pc.Category.IsActive,
                    DisplayOrder = pc.Category.DisplayOrder
                })
                .FirstOrDefault(),
            Variations = product.Variations.Select(v => new ProductVariationDto
            {
                Id = v.Id,
                Name = v.Name,
                Description = v.Description,
                PriceModifier = v.PriceModifier,
                FinalPrice = product.BasePrice + v.PriceModifier,
                IsActive = v.IsActive,
                DisplayOrder = v.DisplayOrder
            }).ToList(),
            SuggestedSideItems = product.SuggestedSideItems.Select(si => new SideItemDto
            {
                Id = si.SideItemProduct.Id,
                Name = si.SideItemProduct.Name,
                Description = si.SideItemProduct.Description,
                Price = si.SideItemProduct.BasePrice,
                IsRequired = si.IsRequired,
                DisplayOrder = si.DisplayOrder
            }).ToList(),
            Content = new()
        };

        foreach (var description in product.Descriptions)
        {
            dto.Content[description.Lang] = new ProductDescriptionDto
            {
                Name = description.Name,
                Description = description.Description
            };
        }
        return dto;
    }
}
