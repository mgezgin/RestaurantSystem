using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Api.Features.Orders.Services;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Queries.GetConfirmedOrdersSinceQuery;

/// <summary>
/// Query to get orders that were confirmed since a specific timestamp.
/// Used for polling fallback to ensure no confirmed orders are missed.
/// </summary>
public record GetConfirmedOrdersSinceQuery(
    DateTime Since
) : IQuery<ApiResponse<ConfirmedOrdersSinceResult>>;

public class ConfirmedOrdersSinceResult
{
    public List<OrderDto> Orders { get; set; } = new();
    public DateTime ServerTime { get; set; }
    public int Count { get; set; }
}

public class GetConfirmedOrdersSinceQueryHandler : IQueryHandler<GetConfirmedOrdersSinceQuery, ApiResponse<ConfirmedOrdersSinceResult>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetConfirmedOrdersSinceQueryHandler> _logger;
    private readonly IOrderMappingService _mappingService;

    public GetConfirmedOrdersSinceQueryHandler(
        ApplicationDbContext context,
        IOrderMappingService mappingService,
        ILogger<GetConfirmedOrdersSinceQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
        _mappingService = mappingService;
    }

    public async Task<ApiResponse<ConfirmedOrdersSinceResult>> Handle(GetConfirmedOrdersSinceQuery query, CancellationToken cancellationToken)
    {
        var since = query.Since;
        var serverTime = DateTime.UtcNow;

        _logger.LogInformation("Polling for confirmed orders since {Since} (server time: {ServerTime})", since, serverTime);

        // Get all confirmed orders that were created or updated after the "since" timestamp
        // This catches both:
        // 1. Orders created as Confirmed (DineIn) after the timestamp
        // 2. Orders that changed to Confirmed after the timestamp
        var orders = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .Include(o => o.DeliveryAddress)
            .Where(o => !o.IsDeleted &&
                       o.Status == OrderStatus.Confirmed &&
                       (o.CreatedAt > since || o.UpdatedAt > since))
            .OrderBy(o => o.OrderDate)
            .Take(100) // Limit to prevent excessive data transfer
            .ToListAsync(cancellationToken);

        var orderDtos = orders.Select(_mappingService.MapToOrderDto).ToList();

        _logger.LogInformation("Found {Count} confirmed order(s) since {Since}", orderDtos.Count, since);

        var result = new ConfirmedOrdersSinceResult
        {
            Orders = orderDtos,
            ServerTime = serverTime,
            Count = orderDtos.Count
        };

        return ApiResponse<ConfirmedOrdersSinceResult>.SuccessWithData(result);
    }
}
