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
    private readonly string _baseUrl;
    private readonly IConfiguration _configuration;

    public GetProductByIdQueryHandler(ApplicationDbContext context, ILogger<GetProductByIdQueryHandler> logger, IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _baseUrl = _configuration["AWS:S3:BaseUrl"]!;
    }

    public async Task<ApiResponse<ProductDto>> Handle(GetProductByIdQuery query, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .IgnoreQueryFilters() // This will load ALL products, including soft-deleted ones
            .AsSplitQuery()
            .Include(p => p.Descriptions)
            .Include(p => p.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Variations.OrderBy(v => v.DisplayOrder))
            .Include(p => p.DetailedIngredients.Where(di => di.IsActive).OrderBy(di => di.DisplayOrder))
                .ThenInclude(di => di.Descriptions)
            .Include(p => p.SuggestedSideItems) // Add soft delete filter here
                .ThenInclude(si => si.SideItemProduct)
                    .ThenInclude(product => product!.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(p => p.Id == query.Id && !p.IsDeleted, cancellationToken); // Also filter the main product
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
            IsSpecial = product.IsSpecial,
            PreparationTimeMinutes = product.PreparationTimeMinutes,
            Type = product.Type,
            Ingredients = product.Ingredients,
            Allergens = product.Allergens,
            DisplayOrder = product.DisplayOrder,
            DetailedIngredients = product.DetailedIngredients
                .Select(di => new ProductIngredientDto
                {
                    Id = di.Id,
                    Name = di.Name,
                    IsOptional = di.IsOptional,
                    Price = di.Price,
                    IsActive = di.IsActive,
                    DisplayOrder = di.DisplayOrder,
                    Content = di.Descriptions.ToDictionary(
                        d => d.LanguageCode,
                        d => new ProductIngredientContentDto
                        {
                            Name = d.Name,
                            Description = d.Description
                        }
                    )
                })
                .ToList(),
            Images = product.Images.Select(i => new ProductImageDto
            {
                Id = i.Id,
                Url = _baseUrl + "/" + i.Url,
                AltText = i.AltText,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder,
                ProductId = i.ProductId
            }).ToList(),
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
                .Where(si => si.SideItemProduct != null) // Add this
                .OrderBy(si => si.DisplayOrder)
                .Select(si => new SideItemDto
                {
                    Id = si.SideItemProduct.Id,
                    Name = si.SideItemProduct.Name,
                    Description = si.SideItemProduct.Description,
                    Price = si.SideItemProduct.BasePrice,
                    IsRequired = si.IsRequired,
                    DisplayOrder = si.DisplayOrder,
                    Images = si.SideItemProduct.Images
                        .Select(i => new ProductImageDto
                        {
                            Id = i.Id,
                            Url = _baseUrl + "/" + i.Url,
                            AltText = i.AltText,
                            IsPrimary = i.IsPrimary,
                            SortOrder = i.SortOrder,
                            ProductId = i.ProductId
                        })
                        .ToList()
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
