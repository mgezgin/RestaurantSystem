using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Settings.Dtos;
using RestaurantSystem.Api.Features.Settings.Interfaces;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Settings.Services;

public class OrderTypeConfigurationService : IOrderTypeConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public OrderTypeConfigurationService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<OrderTypeConfigurationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var configurations = await _context.OrderTypeConfigurations
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        return configurations.Select(c => new OrderTypeConfigurationDto
        {
            OrderType = c.OrderType,
            IsEnabled = c.IsEnabled,
            DisplayOrder = c.DisplayOrder
        }).ToList();
    }

    public async Task<List<OrderType>> GetEnabledOrderTypesAsync(CancellationToken cancellationToken = default)
    {
        var enabledTypes = await _context.OrderTypeConfigurations
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.DisplayOrder)
            .Select(c => c.OrderType)
            .ToListAsync(cancellationToken);

        return enabledTypes;
    }

    public async Task<OrderTypeConfigurationDto> UpdateAsync(
        OrderType orderType, 
        bool isEnabled, 
        CancellationToken cancellationToken = default)
    {
        var configuration = await _context.OrderTypeConfigurations
            .FirstOrDefaultAsync(c => c.OrderType == orderType, cancellationToken);

        if (configuration == null)
        {
            throw new InvalidOperationException($"Order type configuration not found for {orderType}");
        }

        configuration.IsEnabled = isEnabled;
        configuration.UpdatedAt = DateTime.UtcNow;
        configuration.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        return new OrderTypeConfigurationDto
        {
            OrderType = configuration.OrderType,
            IsEnabled = configuration.IsEnabled,
            DisplayOrder = configuration.DisplayOrder
        };
    }
}
