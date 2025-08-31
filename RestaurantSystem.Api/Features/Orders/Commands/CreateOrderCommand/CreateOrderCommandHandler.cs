using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Api.Features.Orders.Services;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Commands.CreateOrderCommand;

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, ApiResponse<OrderDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateOrderCommandHandler> _logger;
    private readonly IOrderEventService _orderEventService;
    private readonly IOrderMappingService _mappingService;

    public CreateOrderCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IOrderEventService orderEventService,
        IOrderMappingService mappingService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _orderEventService = orderEventService;
        _mappingService = mappingService;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderDto>> Handle(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Generate order number
            var orderNumber = await GenerateOrderNumber(cancellationToken);

            var userId = command.UserId ?? _currentUserService.UserId;

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
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
            };


            if (command.Type == OrderType.Delivery)
            {
                var orderAddress = await CreateOrderAddress(command.DeliveryAddress, order.Id, userId, cancellationToken);

                if (orderAddress == null)
                {
                    return ApiResponse<OrderDto>.Failure("Delivery address is required for delivery orders");
                }

                order.DeliveryAddress = orderAddress;
            }



            _context.Orders.Add(order);

            // Process order items and calculate totals
            decimal subTotal = 0;
            foreach (var itemDto in command.Items)
            {

                if (itemDto.MenuId.HasValue)
                {
                    var menu = await _context.Menus
                        .Include(p => p.MenuItems)
                        .FirstOrDefaultAsync(p => p.Id == itemDto.MenuId && !p.IsDeleted, cancellationToken);

                    if (menu == null)
                    {
                        return ApiResponse<OrderDto>.Failure($"Menu {itemDto.MenuId} not found");
                    }

                    decimal unitPrice = menu.BasePrice;
                    string? variationName = null;

                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = itemDto.ProductId,
                        ProductVariationId = itemDto.ProductVariationId,
                        MenuId = itemDto.MenuId,
                        ProductName = menu.Name,
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
                else if (itemDto.ProductId.HasValue)
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
            }

            // Calculate order totals
            order.SubTotal = subTotal;
            order.Tax = CalculateTax(subTotal);
            order.DeliveryFee = command.Type == OrderType.Delivery ? CalculateDeliveryFee() : 0;

            // Apply discount
            if (command.HasUserLimitDiscount && subTotal >= command.UserLimitAmount)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && user.IsDiscountActive)
                {
                    order.DiscountPercentage = user.DiscountPercentage;
                    order.Discount = order.SubTotal * (user.DiscountPercentage / 100);
                }
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
            var orderDto = await _mappingService.MapToOrderDtoAsync(order, cancellationToken);

            await _orderEventService.NotifyOrderCreated(orderDto);

            if (order.IsFocusOrder)
            {
                await _orderEventService.NotifyFocusOrderUpdate(orderDto);
            }

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

    private async Task<OrderAddress?> CreateOrderAddress(
       CreateOrderDeliveryAddressDto? addressDto,
       Guid orderId,
       Guid? userId,
       CancellationToken cancellationToken)
    {
        // Case 1: Use saved address ID
        if (addressDto?.UseAddressId != null)
        {
            var savedAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.Id == addressDto.UseAddressId && !a.IsDeleted, cancellationToken);

            if (savedAddress != null)
            {
                return new OrderAddress
                {
                    OrderId = orderId,
                    UserAddressId = savedAddress.Id,
                    Label = savedAddress.Label,
                    AddressLine1 = savedAddress.AddressLine1,
                    AddressLine2 = savedAddress.AddressLine2,
                    City = savedAddress.City,
                    State = savedAddress.State,
                    PostalCode = savedAddress.PostalCode,
                    Country = savedAddress.Country,
                    Phone = savedAddress.Phone,
                    Latitude = savedAddress.Latitude,
                    Longitude = savedAddress.Longitude,
                    DeliveryInstructions = savedAddress.DeliveryInstructions,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };
            }
        }

        // Case 2: Use provided address details
        if (addressDto != null && !string.IsNullOrEmpty(addressDto.AddressLine1))
        {
            return new OrderAddress
            {
                OrderId = orderId,
                Label = addressDto.Label ?? "Delivery Address",
                AddressLine1 = addressDto.AddressLine1,
                AddressLine2 = addressDto.AddressLine2,
                City = addressDto.City!,
                State = addressDto.State,
                PostalCode = addressDto.PostalCode!,
                Country = addressDto.Country!,
                Phone = addressDto.Phone,
                Latitude = addressDto.Latitude,
                Longitude = addressDto.Longitude,
                DeliveryInstructions = addressDto.DeliveryInstructions,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
            };
        }

        // Case 3: Use customer's default address if no address provided
        if (userId.HasValue)
        {
            var defaultAddress = await _context.UserAddresses
                .FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault && !a.IsDeleted, cancellationToken);

            if (defaultAddress != null)
            {
                _logger.LogInformation("Using customer's default address for order");
                return new OrderAddress
                {
                    OrderId = orderId,
                    UserAddressId = defaultAddress.Id,
                    Label = defaultAddress.Label,
                    AddressLine1 = defaultAddress.AddressLine1,
                    AddressLine2 = defaultAddress.AddressLine2,
                    City = defaultAddress.City,
                    State = defaultAddress.State,
                    PostalCode = defaultAddress.PostalCode,
                    Country = defaultAddress.Country,
                    Phone = defaultAddress.Phone,
                    Latitude = defaultAddress.Latitude,
                    Longitude = defaultAddress.Longitude,
                    DeliveryInstructions = defaultAddress.DeliveryInstructions,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };
            }
        }

        return null;
    }
}
