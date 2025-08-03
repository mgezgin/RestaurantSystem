using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace RestaurantSystem.Api.Features.Orders.Queries.GetOrdersQuery;

public record GetOrdersQuery(
    string? Status,
    string? PaymentStatus,
    string? OrderType,
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? UserId,
    string? Search,
    bool? IsFocusOrder,
    string? OrderBy = "OrderDate",
    bool Descending = true,
    int Page = 1,
    int PageSize = 10
) : IQuery<ApiResponse<PagedResult<OrderDto>>>;

public class GetOrdersQueryHandler : IQueryHandler<GetOrdersQuery, ApiResponse<PagedResult<OrderDto>>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GetOrdersQueryHandler> _logger;

    public GetOrdersQueryHandler(ApplicationDbContext context, ILogger<GetOrdersQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<PagedResult<OrderDto>>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        var ordersQuery = _context.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .Where(o => !o.IsDeleted)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<OrderStatus>(query.Status, out var status))
        {
            ordersQuery = ordersQuery.Where(o => o.Status == status);
        }

        if (!string.IsNullOrEmpty(query.PaymentStatus) && Enum.TryParse<PaymentStatus>(query.PaymentStatus, out var paymentStatus))
        {
            ordersQuery = ordersQuery.Where(o => o.PaymentStatus == paymentStatus);
        }

        if (!string.IsNullOrEmpty(query.OrderType) && Enum.TryParse<OrderType>(query.OrderType, out var orderType))
        {
            ordersQuery = ordersQuery.Where(o => o.Type == orderType);
        }

        if (query.StartDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.OrderDate <= query.EndDate.Value);
        }

        if (query.UserId.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.UserId == query.UserId.Value);
        }

        if (query.IsFocusOrder.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.IsFocusOrder == query.IsFocusOrder.Value);
        }

        if (!string.IsNullOrEmpty(query.Search))
        {
            var searchLower = query.Search.ToLower();
            ordersQuery = ordersQuery.Where(o =>
                o.OrderNumber.ToLower().Contains(searchLower) ||
                (o.CustomerName != null && o.CustomerName.ToLower().Contains(searchLower)) ||
                (o.CustomerEmail != null && o.CustomerEmail.ToLower().Contains(searchLower)) ||
                (o.CustomerPhone != null && o.CustomerPhone.ToLower().Contains(searchLower)));
        }

        // Get total count before pagination
        var totalCount = await ordersQuery.CountAsync(cancellationToken);

        // Apply sorting
        Expression<Func<Order, object>> keySelector = query.OrderBy?.ToLower() switch
        {
            "ordernumber" => o => o.OrderNumber,
            "total" => o => o.Total,
            "status" => o => o.Status,
            "paymentstatus" => o => o.PaymentStatus,
            "customername" => o => o.CustomerName ?? "",
            _ => o => o.OrderDate
        };

        ordersQuery = query.Descending
            ? ordersQuery.OrderByDescending(keySelector)
            : ordersQuery.OrderBy(keySelector);

        // Apply pagination
        var orders = await ordersQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOsa
        var orderDtos = orders.Select(MapToOrderDto).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);

        var pagedResult = new PagedResult<OrderDto>(orderDtos, totalCount, query.Page, query.PageSize, totalPages);

        _logger.LogInformation("Retrieved {Count} orders out of {TotalCount} total", orderDtos.Count, totalCount);

        return ApiResponse<PagedResult<OrderDto>>.SuccessWithData(pagedResult);
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

