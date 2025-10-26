using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Products.Dtos;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Products.Queries.GetFeaturedSpecialQuery;

/// <summary>
/// Query to get the currently featured special product
/// </summary>
public record GetFeaturedSpecialQuery() : IQuery<ApiResponse<FeaturedSpecialDto?>>;

public class GetFeaturedSpecialQueryHandler : IQueryHandler<GetFeaturedSpecialQuery, ApiResponse<FeaturedSpecialDto?>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetFeaturedSpecialQueryHandler> _logger;
    private readonly string _baseUrl;
    private readonly IConfiguration _configuration;

    public GetFeaturedSpecialQueryHandler(
        ApplicationDbContext context,
        ILogger<GetFeaturedSpecialQueryHandler> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
        _baseUrl = _configuration["AWS:S3:BaseUrl"]!;
    }

    public async Task<ApiResponse<FeaturedSpecialDto?>> Handle(
        GetFeaturedSpecialQuery query,
        CancellationToken cancellationToken)
    {
        // Get the product where IsFeaturedSpecial = true
        var featuredProduct = await _context.Products
            .Include(p => p.Images)
            .Where(p => p.IsFeaturedSpecial && p.IsSpecial && p.IsActive)
            .FirstOrDefaultAsync(cancellationToken);

        if (featuredProduct == null)
        {
            _logger.LogInformation("No featured special found");
            return ApiResponse<FeaturedSpecialDto?>.SuccessWithData(null, "No featured special available");
        }

        // Map to DTO
        var featuredSpecialDto = new FeaturedSpecialDto
        {
            Id = featuredProduct.Id,
            Name = featuredProduct.Name,
            Description = featuredProduct.Description,
            BasePrice = featuredProduct.BasePrice,
            ImageUrl = featuredProduct.Images
                .Where(img => img.IsPrimary)
                .Select(img => _baseUrl + "/" + img.Url)
                .FirstOrDefault() ?? featuredProduct.ImageUrl,
            FeaturedDate = featuredProduct.FeaturedDate ?? DateTime.UtcNow,
            PreparationTimeMinutes = featuredProduct.PreparationTimeMinutes,
            Ingredients = featuredProduct.Ingredients,
            Allergens = featuredProduct.Allergens,
            Images = featuredProduct.Images.Select(img => new ProductImageDto
            {
                Id = img.Id,
                Url = _baseUrl + "/" + img.Url,
                IsPrimary = img.IsPrimary,
                SortOrder = img.SortOrder,
                AltText = img.AltText
            }).ToList()
        };

        _logger.LogInformation(
            "Retrieved featured special: {ProductName} (ID: {ProductId})",
            featuredProduct.Name, featuredProduct.Id);

        return ApiResponse<FeaturedSpecialDto?>.SuccessWithData(
            featuredSpecialDto,
            "Featured special retrieved successfully");
    }
}
