using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Products.Queries.GetFocusOrdersQuery;

public class GetFocusOrdersQuery : IQuery<ApiResponse<List<OrderDto>>>
{
    public bool? ActiveOnly { get; set; } = true;
    public int? Priority { get; set; }
    public string? OrderBy { get; set; } = "Priority"; // Priority, OrderDate, FocusedAt
}

public class GetFocusOrdersQueryHandler : IQueryHandler<GetFocusOrdersQuery, ApiResponse<List<OrderDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetFocusOrdersQueryHandler> _logger;

    public GetFocusOrdersQueryHandler(
        ApplicationDbContext context,
        ILogger<GetFocusOrdersQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<OrderDto>>> Handle(GetFocusOrdersQuery query, CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Where(o => !o.IsDeleted && o.IsFocusOrder);

        // Filter by active status
        if (query.ActiveOnly == true)
        {
            ordersQuery = ordersQuery.Where(o =>
                o.Status != OrderStatus.Completed &&
                o.Status != OrderStatus.Cancelled);
        }

        // Filter by priority
        if (query.Priority.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.Priority == query.Priority.Value);
        }

        // Apply ordering
        ordersQuery = query.OrderBy?.ToLower() switch
        {
            "priority" => ordersQuery
                .OrderBy(o => o.Priority ?? 999)
                .ThenBy(o => o.FocusedAt),
            "orderdate" => ordersQuery.OrderByDescending(o => o.OrderDate),
            "focusedat" => ordersQuery.OrderByDescending(o => o.FocusedAt),
            _ => ordersQuery.OrderBy(o => o.Priority ?? 999).ThenBy(o => o.FocusedAt)
        };

        var orders = await ordersQuery.ToListAsync(cancellationToken);

        var orderDtos = orders.Select(o => new OrderDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            UserId = o.UserId,
            CustomerName = o.CustomerName,
            CustomerEmail = o.CustomerEmail,
            CustomerPhone = o.CustomerPhone,
            Type = o.Type.ToString(),
            TableNumber = o.TableNumber,
            SubTotal = o.SubTotal,
            Tax = o.Tax,
            DeliveryFee = o.DeliveryFee,
            Discount = o.Discount,
            DiscountPercentage = o.DiscountPercentage,
            Tip = o.Tip,
            Total = o.Total,
            TotalPaid = o.TotalPaid,
            RemainingAmount = o.RemainingAmount,
            IsFullyPaid = o.IsFullyPaid,
            Status = o.Status.ToString(),
            PaymentStatus = o.PaymentStatus.ToString(),
            IsFocusOrder = o.IsFocusOrder,
            Priority = o.Priority,
            FocusReason = o.FocusReason,
            FocusedAt = o.FocusedAt,
            FocusedBy = o.FocusedBy,
            OrderDate = o.OrderDate,
            EstimatedDeliveryTime = o.EstimatedDeliveryTime,
            ActualDeliveryTime = o.ActualDeliveryTime,
            Notes = o.Notes,
            DeliveryAddress = o.DeliveryAddress,
            Items = o.Items.Select(i => new OrderItemDto
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
            Payments = o.Payments.Select(p => new OrderPaymentDto
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
            StatusHistory = new List<OrderStatusHistoryDto>()
        }).ToList();

        _logger.LogInformation("Retrieved {Count} focus orders", orderDtos.Count);

        return ApiResponse<List<OrderDto>>.SuccessWithData(orderDtos,
            $"Retrieved {orderDtos.Count} focus orders");
    }
}
