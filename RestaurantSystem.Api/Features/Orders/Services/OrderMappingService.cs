using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Services;

public class OrderMappingService : IOrderMappingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderMappingService> _logger;

    public OrderMappingService(ApplicationDbContext context, ILogger<OrderMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public OrderDto MapToOrderDto(Order order)
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
            CustomerDiscountAmount = order.CustomerDiscountAmount,
            Tip = order.Tip,
            Total = order.Total,
            TotalPaid = order.TotalPaid,
            RemainingAmount = order.RemainingAmount,
            IsFullyPaid = order.IsFullyPaid,
            PromoCode = order.PromoCode,
            HasUserLimitDiscount = order.HasUserLimitDiscount,
            UserLimitAmount = order.UserLimitAmount,
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
            CancellationReason = order.CancellationReason,
            DeliveryAddress = MapToDeliveryAddressDto(order.DeliveryAddress),
            Items = order.Items?.Select(MapToOrderItemDto).ToList() ?? new List<OrderItemDto>(),
            Payments = order.Payments?.Select(MapToOrderPaymentDto).ToList() ?? new List<OrderPaymentDto>(),
            StatusHistory = order.StatusHistory?.Select(sh => new OrderStatusHistoryDto
            {
                Id = sh.Id,
                FromStatus = sh.FromStatus.ToString(),
                ToStatus = sh.ToStatus.ToString(),
                Notes = sh.Notes,
                ChangedAt = sh.CreatedAt,
                ChangedBy = sh.CreatedBy
            }).ToList() ?? new List<OrderStatusHistoryDto>(),
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt
        };
    }

    public OrderSummaryDto MapToOrderSummaryDto(Order order)
    {
        return new OrderSummaryDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerName = order.CustomerName,
            Type = order.Type.ToString(),
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            Total = order.Total,
            OrderDate = order.OrderDate,
            ItemCount = order.Items?.Count ?? 0,
            IsFocusOrder = order.IsFocusOrder
        };
    }

    public OrderItemDto MapToOrderItemDto(OrderItem item)
    {
        return new OrderItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductVariationId = item.ProductVariationId,
            MenuID = item.MenuId,
            ProductName = item.ProductName,
            VariationName = item.VariationName,
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            ItemTotal = item.ItemTotal,
            SpecialInstructions = item.SpecialInstructions,
            KitchenType = item.Product?.KitchenType.ToString()
        };
    }

    public OrderPaymentDto MapToOrderPaymentDto(OrderPayment payment)
    {
        return new OrderPaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            PaymentMethod = payment.PaymentMethod.ToString(),
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            TransactionId = payment.TransactionId,
            PaymentDate = payment.PaymentDate,
            RefundedAmount = payment.RefundedAmount,
            RefundDate = payment.RefundDate,
            RefundReason = payment.RefundReason,
            CreatedAt = payment.CreatedAt
        };
    }

    public DeliveryAddressDto? MapToDeliveryAddressDto(OrderAddress? address)
    {
        if (address == null) return null;

        return new DeliveryAddressDto
        {
            Id = address.Id,
            OrderId = address.OrderId,
            UserAddressId = address.UserAddressId,
            Label = address.Label,
            AddressLine1 = address.AddressLine1,
            AddressLine2 = address.AddressLine2,
            City = address.City,
            State = address.State,
            PostalCode = address.PostalCode,
            Country = address.Country,
            Phone = address.Phone,
            Latitude = address.Latitude,
            Longitude = address.Longitude,
            DeliveryInstructions = address.DeliveryInstructions,
            FullAddress = address.GetFullAddress()
        };
    }

    public async Task<OrderDto> MapToOrderDtoAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Load related data if not already loaded
        if (!_context.Entry(order).Collection(o => o.Items).IsLoaded)
        {
            await _context.Entry(order).Collection(o => o.Items).LoadAsync(cancellationToken);
        }

        // Load Product for each item to access KitchenType
        if (order.Items != null)
        {
            foreach (var item in order.Items)
            {
                if (!_context.Entry(item).Reference(i => i.Product).IsLoaded)
                {
                    await _context.Entry(item).Reference(i => i.Product).LoadAsync(cancellationToken);
                }
            }
        }

        if (!_context.Entry(order).Collection(o => o.Payments).IsLoaded)
        {
            await _context.Entry(order).Collection(o => o.Payments).LoadAsync(cancellationToken);
        }

        if (!_context.Entry(order).Collection(o => o.StatusHistory).IsLoaded)
        {
            await _context.Entry(order).Collection(o => o.StatusHistory).LoadAsync(cancellationToken);
        }

        if (!_context.Entry(order).Reference(o => o.DeliveryAddress).IsLoaded)
        {
            await _context.Entry(order).Reference(o => o.DeliveryAddress).LoadAsync(cancellationToken);
        }

        return MapToOrderDto(order);
    }
}