using MediatR;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Commands.DeleteOrderCommand;

public record DeleteOrderCommand(Guid OrderId) : ICommand<ApiResponse<bool>>;

public class DeleteOrderCommandHandler : ICommandHandler<DeleteOrderCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteOrderCommandHandler> _logger;

    public DeleteOrderCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteOrderCommand command, CancellationToken cancellationToken)
    {
        // Find the order (including soft-deleted for proper handling)
        var order = await _context.Orders
            .IgnoreQueryFilters() // Include soft-deleted to prevent re-deletion
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, cancellationToken);

        if (order == null)
        {
            return ApiResponse<bool>.Failure("Order not found");
        }

        if (order.IsDeleted)
        {
            return ApiResponse<bool>.Failure("Order has already been deleted");
        }

        // Soft delete the order
        order.IsDeleted = true;
        order.DeletedAt = DateTime.UtcNow;
        order.DeletedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Order {OrderNumber} (ID: {OrderId}) deleted by user {UserId}",
            order.OrderNumber,
            order.Id,
            _currentUserService.UserId);

        return ApiResponse<bool>.SuccessWithData(true, "Order deleted successfully");
    }
}
