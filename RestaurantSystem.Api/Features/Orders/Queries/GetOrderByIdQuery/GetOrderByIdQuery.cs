using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace RestaurantSystem.Api.Features.Orders.Queries.GetOrderByIdQuery;

public record GetOrderByIdQuery(Guid Id) : IQuery<ApiResponse<OrderDto>>;

public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, ApiResponse<OrderDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(ApplicationDbContext context, ILogger<GetOrderByIdQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDto>> Handle(GetOrderByIdQuery query, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
                .ThenInclude(i => i.Product)
            .Include(o => o.Items)
                .ThenInclude(i => i.ProductVariation)
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == query.Id && !o.IsDeleted, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Order with ID {OrderId} not found", query.Id);
            return ApiResponse<OrderDto>.Failure("Order not found");
        }

        var orderDto = MapToOrderDto(order);

        _logger.LogInformation("Retrieved order {OrderNumber} with ID {OrderId}", order.OrderNumber, query.Id);

        return ApiResponse<OrderDto>.SuccessWithData(orderDto);
    }

    private static OrderDto MapToOrderDto(Order order)
    {
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
            CancellationReason = order.CancellationReason,
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
            }).OrderByDescending(h => h.ChangedAt).ToList()
        };
    }
}

