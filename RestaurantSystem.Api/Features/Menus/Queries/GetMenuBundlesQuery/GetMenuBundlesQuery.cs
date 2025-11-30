using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Menus.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using MediatR; // Added for MediatR

namespace RestaurantSystem.Api.Features.Menus.Queries.GetMenuBundlesQuery;

public record GetMenuBundlesQuery(int Page, int PageSize, Guid? CategoryId = null) : IQuery<ApiResponse<PagedResult<MenuBundleDto>>>;

public class GetMenuBundlesQueryHandler(ApplicationDbContext context, IConfiguration configuration)
    : IQueryHandler<GetMenuBundlesQuery, ApiResponse<PagedResult<MenuBundleDto>>>
{
    private readonly ApplicationDbContext _context = context;
    private readonly string _baseUrl = configuration["AWS:S3:BaseUrl"]!;
    // The original _logger field and its injection via the constructor are removed as per the primary constructor syntax in the provided change.

    public async Task<ApiResponse<PagedResult<MenuBundleDto>>> Handle(GetMenuBundlesQuery query, CancellationToken cancellationToken)
    {
        var queryable = _context.Products
            .Include(p => p.MenuDefinition)
            .Include(p => p.Descriptions)
            .Include(p => p.Images)
            .Where(p => !p.IsDeleted && p.Type == ProductType.Menu);

        var totalCount = await queryable.CountAsync(cancellationToken);

        var products = await queryable
            .OrderBy(p => p.DisplayOrder)
            .ThenBy(p => p.Name)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = products.Select(MapToMenuBundleDto).ToList();

        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var result = new PagedResult<MenuBundleDto>(
            dtos,
            totalCount,
            query.Page,
            query.PageSize,
            totalPages
        );

        return ApiResponse<PagedResult<MenuBundleDto>>.SuccessWithData(result,
            $"Retrieved {products.Count} menu bundles");
    }

    private MenuBundleDto MapToMenuBundleDto(Product product)
    {
        var dto = new MenuBundleDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            IsActive = product.IsActive,
            IsAvailable = product.IsAvailable,
            IsSpecial = product.IsSpecial,
            PreparationTimeMinutes = product.PreparationTimeMinutes,
            Type = "menu",
            DisplayOrder = product.DisplayOrder,
            MenuDefinition = product.MenuDefinition != null ? new MenuDefinitionDto
            {
                Id = product.MenuDefinition.Id,
                IsAlwaysAvailable = product.MenuDefinition.IsAlwaysAvailable,
                StartTime = product.MenuDefinition.StartTime?.ToString(@"hh\:mm\:ss"),
                EndTime = product.MenuDefinition.EndTime?.ToString(@"hh\:mm\:ss"),
                AvailableMonday = product.MenuDefinition.AvailableMonday,
                AvailableTuesday = product.MenuDefinition.AvailableTuesday,
                AvailableWednesday = product.MenuDefinition.AvailableWednesday,
                AvailableThursday = product.MenuDefinition.AvailableThursday,
                AvailableFriday = product.MenuDefinition.AvailableFriday,
                AvailableSaturday = product.MenuDefinition.AvailableSaturday,
                AvailableSunday = product.MenuDefinition.AvailableSunday,
            } : null,
            Content = new(),
            Images = product.Images.Select(i => new RestaurantSystem.Api.Features.Products.Dtos.ProductImageDto
            {
                Id = i.Id,
                Url = _baseUrl + "/" + i.Url,
                AltText = i.AltText,
                IsPrimary = i.IsPrimary,
                SortOrder = i.SortOrder
            }).OrderBy(i => i.SortOrder).ToList()
        };

        foreach (var description in product.Descriptions)
        {
            dto.Content[description.Lang] = new MenuBundleContentDto
            {
                Name = description.Name,
                Description = description.Description
            };
        }
        return dto;
    }
}
