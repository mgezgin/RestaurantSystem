using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Categories.Dtos;
using RestaurantSystem.Api.Features.Products.Dtos;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Products.Queries.GetProductByIdQuery;

public record GetProductByIdQuery(Guid Id) : IQuery<ApiResponse<ProductDto>>;

public class GetProductByIdQueryHandler : IQueryHandler<GetProductByIdQuery, ApiResponse<ProductDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetProductByIdQueryHandler> _logger;

    public GetProductByIdQueryHandler(ApplicationDbContext context, ILogger<GetProductByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variations.Where(v => !v.IsDeleted).OrderBy(v => v.DisplayOrder))
            .Include(p => p.SuggestedSideItems)
                .ThenInclude(si => si.SideItemProduct)
            .FirstOrDefaultAsync(p => p.Id == query.Id, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", query.Id);
            return ApiResponse<ProductDto>.Failure("Product not found");
        }

        var productDto = new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            IsActive = product.IsActive,
            IsAvailable = product.IsAvailable,
            PreparationTimeMinutes = product.PreparationTimeMinutes,
            Type = product.Type,
            Ingredients =product.Ingredients,
            Allergens = product.Allergens,
            DisplayOrder = product.DisplayOrder,
            Categories = product.ProductCategories
                .OrderBy(pc => pc.DisplayOrder)
                .Select(pc => new ProductCategoryDto
                {
                    CategoryId = pc.CategoryId,
                    CategoryName = pc.Category.Name,
                    IsPrimary = pc.IsPrimary,
                    DisplayOrder = pc.DisplayOrder
                })
                .ToList(),
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
            Variations = product.Variations
                .Where(v => v.IsActive)
                .Select(v => new ProductVariationDto
                {
                    Id = v.Id,
                    Name = v.Name,
                    Description = v.Description,
                    PriceModifier = v.PriceModifier,
                    FinalPrice = product.BasePrice + v.PriceModifier,
                    IsActive = v.IsActive,
                    DisplayOrder = v.DisplayOrder
                })
                .ToList(),
            SuggestedSideItems = product.SuggestedSideItems
                .OrderBy(si => si.DisplayOrder)
                .Select(si => new SideItemDto
                {
                    Id = si.SideItemProduct.Id,
                    Name = si.SideItemProduct.Name,
                    Description = si.SideItemProduct.Description,
                    Price = si.SideItemProduct.BasePrice,
                    IsRequired = si.IsRequired,
                    DisplayOrder = si.DisplayOrder
                })
                .ToList(),
            Content = new()
        };

        foreach (var description in product.Descriptions)
        {
            productDto.Content[description.Lang] = new ProductDescriptionDto
            {
                Name = description.Name,
                Description = description.Description
            };
        }

        _logger.LogInformation("Retrieved product {ProductId} successfully", query.Id);
        return ApiResponse<ProductDto>.SuccessWithData(productDto);
    }
}
