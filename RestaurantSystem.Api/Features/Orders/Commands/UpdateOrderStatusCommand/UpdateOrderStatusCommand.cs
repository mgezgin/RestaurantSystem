using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Commands.UpdateOrderStatusCommand;

public record UpdateOrderStatusCommand : ICommand<ApiResponse<OrderDto>>
{
    public Guid OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOrderStatusCommandHandler : ICommandHandler<UpdateOrderStatusCommand, ApiResponse<OrderDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;

    public UpdateOrderStatusCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateOrderStatusCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDto>> Handle(UpdateOrderStatusCommand command, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId && !o.IsDeleted, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderDto>.Failure("Order not found");
        }

        // Validate status transition
        if (!IsValidStatusTransition(order.Status, command.NewStatus))
        {
            return ApiResponse<OrderDto>.Failure($"Cannot transition from {order.Status} to {command.NewStatus}");
        }

        // Add status history
        var statusHistory = new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = order.Status,
            ToStatus = command.NewStatus,
            Notes = command.Notes,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = _currentUserService.UserId?.ToString() ?? "System",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
        };

        _context.OrderStatusHistories.Add(statusHistory);

        // Update order status
        order.Status = command.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        // Handle specific status changes
        switch (command.NewStatus)
        {
            case OrderStatus.Completed:
                order.ActualDeliveryTime = DateTime.UtcNow;
                break;

            case OrderStatus.Preparing:
                // Update estimated time if needed
                if (order.Type == OrderType.Delivery && !order.EstimatedDeliveryTime.HasValue)
                {
                    order.EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45);
                }
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var orderDto = MapToOrderDto(order);

        _logger.LogInformation("Order {OrderNumber} status updated from {FromStatus} to {ToStatus} by user {UserId}",
            order.OrderNumber, statusHistory.FromStatus, statusHistory.ToStatus, _currentUserService.UserId);

        return ApiResponse<OrderDto>.SuccessWithData(orderDto, "Order status updated successfully");
    }

    private bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return currentStatus switch
        {
            OrderStatus.Pending => newStatus is OrderStatus.Confirmed or OrderStatus.Cancelled,
            OrderStatus.Confirmed => newStatus is OrderStatus.Preparing or OrderStatus.Cancelled,
            OrderStatus.Preparing => newStatus is OrderStatus.Ready or OrderStatus.Cancelled,
            OrderStatus.Ready => newStatus is OrderStatus.OutForDelivery or OrderStatus.Completed or OrderStatus.Cancelled,
            OrderStatus.OutForDelivery => newStatus is OrderStatus.Completed or OrderStatus.Cancelled,
            OrderStatus.Completed => false, // Cannot change from completed
            OrderStatus.Cancelled => false, // Cannot change from cancelled
            _ => false
        };
    }

    private static OrderDto MapToOrderDto(Order order)
    {
        // Same mapping logic as before
        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            Type = order.Type.ToString(),
            TableNumber = order.TableNumber,
            SubTotal = order.SubTotal,
            Tax = order.Tax,
            DeliveryFee = order.DeliveryFee,
            Discount = order.Discount,
            DiscountPercentage = order.DiscountPercentage,
            Tip = order.Tip,
            Total = order.Total,
            TotalPaid = order.TotalPaid,
            RemainingAmount = order.RemainingAmount,
            IsFullyPaid = order.IsFullyPaid,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            IsFocusOrder = order.IsFocusOrder,
            Priority = order.Priority,
            FocusReason = order.FocusReason,
            FocusedAt = order.FocusedAt,
            FocusedBy = order.FocusedBy,
            OrderDate = order.OrderDate,
            EstimatedDeliveryTime = order.EstimatedDeliveryTime,
            ActualDeliveryTime = order.ActualDeliveryTime,
            Notes = order.Notes,
            DeliveryAddress = order.DeliveryAddress,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductVariationId = i.ProductVariationId,
                ProductName = i.ProductName,
                VariationName = i.VariationName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                ItemTotal = i.ItemTotal,
                SpecialInstructions = i.SpecialInstructions
            }).ToList(),
            Payments = order.Payments.Select(p => new OrderPaymentDto
            {
                Id = p.Id,
                OrderId = p.OrderId,
                PaymentMethod = p.PaymentMethod.ToString(),
                Amount = p.Amount,
                Status = p.Status.ToString(),
                TransactionId = p.TransactionId,
                ReferenceNumber = p.ReferenceNumber,
                PaymentDate = p.PaymentDate,
                CardLastFourDigits = p.CardLastFourDigits,
                CardType = p.CardType,
                PaymentGateway = p.PaymentGateway,
                PaymentNotes = p.PaymentNotes,
                IsRefunded = p.IsRefunded,
                RefundedAmount = p.RefundedAmount,
                RefundDate = p.RefundDate,
                RefundReason = p.RefundReason
            }).ToList(),
            StatusHistory = order.StatusHistory.Select(h => new OrderStatusHistoryDto
            {
                Id = h.Id,
                FromStatus = h.FromStatus.ToString(),
                ToStatus = h.ToStatus.ToString(),
                Notes = h.Notes,
                ChangedAt = h.ChangedAt,
                ChangedBy = h.ChangedBy
            }).ToList()
        };
    }
}
