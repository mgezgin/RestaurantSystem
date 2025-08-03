using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Commands.CreateOrderCommand;

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, ApiResponse<OrderDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;

    public CreateOrderCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDto>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Generate order number
            var orderNumber = await GenerateOrderNumber(cancellationToken);

            // Create order
            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = command.UserId ?? _currentUserService.UserId,
                CustomerName = command.CustomerName,
                CustomerEmail = command.CustomerEmail,
                CustomerPhone = command.CustomerPhone,
                Type = command.Type,
                TableNumber = command.TableNumber,
                PromoCode = command.PromoCode,
                HasUserLimitDiscount = command.HasUserLimitDiscount,
                UserLimitAmount = command.UserLimitAmount,
                IsFocusOrder = command.IsFocusOrder,
                Priority = command.Priority,
                FocusReason = command.FocusReason,
                FocusedAt = command.IsFocusOrder ? DateTime.UtcNow : null,
                FocusedBy = command.IsFocusOrder ? _currentUserService.UserId?.ToString() : null,
                Notes = command.Notes,
                DeliveryAddress = command.DeliveryAddress,
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
            };

            _context.Orders.Add(order);

            // Process order items and calculate totals
            decimal subTotal = 0;
            foreach (var itemDto in command.Items)
            {
                var product = await _context.Products
                    .Include(p => p.Variations)
                    .FirstOrDefaultAsync(p => p.Id == itemDto.ProductId && !p.IsDeleted, cancellationToken);

                if (product == null)
                {
                    return ApiResponse<OrderDto>.Failure($"Product {itemDto.ProductId} not found");
                }

                decimal unitPrice = product.BasePrice;
                string? variationName = null;

                if (itemDto.ProductVariationId.HasValue)
                {
                    var variation = product.Variations
                        .FirstOrDefault(v => v.Id == itemDto.ProductVariationId.Value && !v.IsDeleted);

                    if (variation == null)
                    {
                        return ApiResponse<OrderDto>.Failure($"Product variation {itemDto.ProductVariationId} not found");
                    }

                    unitPrice += variation.PriceModifier;
                    variationName = variation.Name;
                }

                var orderItem = new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = itemDto.ProductId,
                    ProductVariationId = itemDto.ProductVariationId,
                    MenuId = itemDto.MenuId,
                    ProductName = product.Name,
                    VariationName = variationName,
                    Quantity = itemDto.Quantity,
                    UnitPrice = unitPrice,
                    ItemTotal = unitPrice * itemDto.Quantity,
                    SpecialInstructions = itemDto.SpecialInstructions,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };

                _context.OrderItems.Add(orderItem);
                order.Items.Add(orderItem);
                subTotal += orderItem.ItemTotal;
            }

            // Calculate order totals
            order.SubTotal = subTotal;
            order.Tax = CalculateTax(subTotal);
            order.DeliveryFee = command.Type == OrderType.Delivery ? CalculateDeliveryFee() : 0;

            // Apply discount
            if (command.HasUserLimitDiscount && subTotal >= command.UserLimitAmount)
            {
                order.DiscountPercentage = 10; // Example: 10% discount
                order.Discount = subTotal * (order.DiscountPercentage / 100);
            }

            order.Total = order.SubTotal + order.Tax + order.DeliveryFee - order.Discount;

            // Process payments
            decimal totalPaid = 0;
            foreach (var paymentDto in command.Payments)
            {
                var payment = new OrderPayment
                {
                    OrderId = order.Id,
                    PaymentMethod = paymentDto.PaymentMethod,
                    Amount = paymentDto.Amount,
                    Status = PaymentStatus.Pending,
                    TransactionId = paymentDto.TransactionId,
                    ReferenceNumber = paymentDto.ReferenceNumber,
                    CardLastFourDigits = paymentDto.CardLastFourDigits,
                    CardType = paymentDto.CardType,
                    PaymentGateway = paymentDto.PaymentGateway,
                    PaymentNotes = paymentDto.PaymentNotes,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };

                _context.OrderPayments.Add(payment);
                order.Payments.Add(payment);
                totalPaid += payment.Amount;
            }

            // Update payment summary
            order.TotalPaid = totalPaid;
            order.RemainingAmount = order.Total - totalPaid;

            // Update payment status based on payments
            if (order.RemainingAmount <= 0)
            {
                order.PaymentStatus = order.RemainingAmount < 0 ? PaymentStatus.Overpaid : PaymentStatus.Completed;
                // Process any immediate payments (e.g., credit card)
                foreach (var payment in order.Payments.Where(p => p.PaymentMethod != PaymentMethod.Cash))
                {
                    payment.Status = PaymentStatus.Completed;
                    // Here you would integrate with payment gateways
                }
            }
            else if (totalPaid > 0)
            {
                order.PaymentStatus = PaymentStatus.PartiallyPaid;
            }

            // Add initial status history
            var statusHistory = new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Pending,
                ToStatus = OrderStatus.Pending,
                Notes = "Order created",
                ChangedAt = DateTime.UtcNow,
                ChangedBy = _currentUserService.UserId?.ToString() ?? "System",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
            };
            _context.OrderStatusHistories.Add(statusHistory);

            // Calculate estimated delivery time
            if (command.Type == OrderType.Delivery)
            {
                order.EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45); // Example: 45 minutes
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Map to DTO
            var orderDto = MapToOrderDto(order);

            _logger.LogInformation("Order {OrderNumber} created successfully by user {UserId}",
                order.OrderNumber, _currentUserService.UserId);

            return ApiResponse<OrderDto>.SuccessWithData(orderDto, "Order created successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating order");
            throw;
        }
    }

    private async Task<string> GenerateOrderNumber(CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyyMMdd");
        var lastOrder = await _context.Orders
            .Where(o => o.OrderNumber.StartsWith(date))
            .OrderByDescending(o => o.OrderNumber)
            .FirstOrDefaultAsync(cancellationToken);

        int sequence = 1;
        if (lastOrder != null)
        {
            var lastSequence = lastOrder.OrderNumber.Substring(8);
            if (int.TryParse(lastSequence, out var seq))
            {
                sequence = seq + 1;
            }
        }

        return $"{date}{sequence:D4}";
    }

    private decimal CalculateTax(decimal subTotal)
    {
        const decimal taxRate = 0.18m; // 18% tax rate
        return Math.Round(subTotal * taxRate, 2);
    }

    private decimal CalculateDeliveryFee()
    {
        return 5.00m; // Fixed delivery fee, could be dynamic based on distance
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
            StatusHistory = new List<OrderStatusHistoryDto>()
        };
    }
}
