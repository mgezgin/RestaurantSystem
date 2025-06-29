using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Products.Dtos;
using RestaurantSystem.Api.Features.Products.Queries.GetProductByIdQuery;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Products.Commands.UpdateProductCommand;

public record UpdateProductCommand(
    Guid Id,
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
    List<UpdateProductVariationDto>? Variations,
    List<Guid>? SuggestedSideItemIds
) : ICommand<ApiResponse<ProductDto>>;

public class UpdateProductCommandHandler : ICommandHandler<UpdateProductCommand, ApiResponse<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateProductCommandHandler> _logger;
    private readonly ILogger<GetProductByIdQueryHandler> _getProductlogger;


    public UpdateProductCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateProductCommandHandler> logger,
        ILogger<GetProductByIdQueryHandler> getProductlogger


        )
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
        _getProductlogger = getProductlogger;
    }

    public async Task<ApiResponse<ProductDto>> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.Variations)
            .Include(p => p.SuggestedSideItems)
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (product == null)
        {
            return ApiResponse<ProductDto>.Failure("Product not found");
        }

        // Validate categories
        var categories = await _context.Categories
            .Where(c => command.CategoryIds.Contains(c.Id))
            .ToListAsync(cancellationToken);

        if (categories.Count != command.CategoryIds.Count)
        {
            return ApiResponse<ProductDto>.Failure("One or more categories not found");
        }

        // Update product properties
        product.Name = command.Name;
        product.Description = command.Description;
        product.BasePrice = command.BasePrice;
        product.IsActive = command.IsActive;
        product.IsAvailable = command.IsAvailable;
        product.PreparationTimeMinutes = command.PreparationTimeMinutes;
        product.Type = command.Type;
        product.Ingredients = command.Ingredients.Any()
            ? System.Text.Json.JsonSerializer.Serialize(command.Ingredients)
            : null;
        product.Allergens = command.Allergens.Any()
            ? System.Text.Json.JsonSerializer.Serialize(command.Allergens)
            : null;
        product.DisplayOrder = command.DisplayOrder;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        // Update categories
        _context.ProductCategories.RemoveRange(product.ProductCategories);

        var displayOrder = 0;
        foreach (var categoryId in command.CategoryIds)
        {
            var productCategory = new ProductCategory
            {
                ProductId = product.Id,
                CategoryId = categoryId,
                IsPrimary = categoryId == command.PrimaryCategoryId,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
            };
            _context.ProductCategories.Add(productCategory);
        }

        // Update variations
        if (command.Variations != null)
        {
            // Handle existing variations
            foreach (var variation in product.Variations)
            {
                var updateDto = command.Variations.FirstOrDefault(v => v.Id == variation.Id);
                if (updateDto != null)
                {
                    variation.Name = updateDto.Name;
                    variation.Description = updateDto.Description;
                    variation.PriceModifier = updateDto.PriceModifier;
                    variation.IsActive = updateDto.IsActive;
                    variation.DisplayOrder = updateDto.DisplayOrder;
                    variation.UpdatedAt = DateTime.UtcNow;
                    variation.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";
                }
                else
                {
                    // Mark as deleted if not in update list
                    variation.IsDeleted = true;
                    variation.DeletedAt = DateTime.UtcNow;
                    variation.DeletedBy = _currentUserService.UserId?.ToString() ?? "System";
                }
            }

            // Add new variations
            var newVariations = command.Variations.Where(v => v.Id == null || v.Id == Guid.Empty);
            foreach (var newVariation in newVariations)
            {
                var variation = new ProductVariation
                {
                    ProductId = product.Id,
                    Name = newVariation.Name,
                    Description = newVariation.Description,
                    PriceModifier = newVariation.PriceModifier,
                    IsActive = newVariation.IsActive,
                    DisplayOrder = newVariation.DisplayOrder,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };
                _context.ProductVariations.Add(variation);
            }
        }

        // Update suggested side items
        if (command.SuggestedSideItemIds != null)
        {
            _context.ProductSideItems.RemoveRange(product.SuggestedSideItems);

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
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Reload for response
        var updatedProduct = await _context.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variations.Where(v => !v.IsDeleted))
            .Include(p => p.SuggestedSideItems)
                .ThenInclude(si => si.SideItemProduct)
            .FirstAsync(p => p.Id == product.Id, cancellationToken);

        var handler = new GetProductByIdQueryHandler(_context, _getProductlogger);
        var result = await handler.Handle(new GetProductByIdQuery(product.Id), cancellationToken);

        _logger.LogInformation("Product {ProductId} updated successfully by user {UserId}",
            product.Id, _currentUserService.UserId);

        return result;
    }
}