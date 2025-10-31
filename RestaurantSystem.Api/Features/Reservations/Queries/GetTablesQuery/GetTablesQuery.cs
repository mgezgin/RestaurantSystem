using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Reservations.Dtos;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Reservations.Queries.GetTablesQuery;

public record GetTablesQuery(
    bool? IsActive = null,
    bool? IsOutdoor = null
) : IQuery<ApiResponse<List<TableDto>>>;

public class GetTablesQueryHandler : IQueryHandler<GetTablesQuery, ApiResponse<List<TableDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetTablesQueryHandler> _logger;

    public GetTablesQueryHandler(ApplicationDbContext context, ILogger<GetTablesQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<TableDto>>> Handle(GetTablesQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var tablesQuery = _context.Tables.AsQueryable();

            if (query.IsActive.HasValue)
            {
                tablesQuery = tablesQuery.Where(t => t.IsActive == query.IsActive.Value);
            }

            if (query.IsOutdoor.HasValue)
            {
                tablesQuery = tablesQuery.Where(t => t.IsOutdoor == query.IsOutdoor.Value);
            }

            var tables = await tablesQuery
                .OrderBy(t => t.TableNumber)
                .Select(t => new TableDto
                {
                    Id = t.Id,
                    TableNumber = t.TableNumber,
                    MaxGuests = t.MaxGuests,
                    IsActive = t.IsActive,
                    IsOutdoor = t.IsOutdoor,
                    PositionX = t.PositionX,
                    PositionY = t.PositionY,
                    Width = t.Width,
                    Height = t.Height,
                    Shape = t.Shape,
                    Notes = t.Notes
                })
                .ToListAsync(cancellationToken);

            return ApiResponse<List<TableDto>>.SuccessWithData(tables);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tables");
            return ApiResponse<List<TableDto>>.Failure("Failed to retrieve tables");
        }
    }
}
