using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Common.Utilities;
using RestaurantSystem.Api.Features.FidelityPoints.Interfaces;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Api.Features.Orders.Services;
using RestaurantSystem.Api.Features.Settings.Interfaces;
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
    private readonly IFidelityPointsService _fidelityPointsService;
    private readonly ICustomerDiscountService _customerDiscountService;
    private readonly ITaxConfigurationService _taxConfigurationService;

    public CreateOrderCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IOrderEventService orderEventService,
        IOrderMappingService mappingService,
        IFidelityPointsService fidelityPointsService,
        ICustomerDiscountService customerDiscountService,
        ITaxConfigurationService taxConfigurationService,
        ILogger<CreateOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _orderEventService = orderEventService;
        _mappingService = mappingService;
        _fidelityPointsService = fidelityPointsService;
        _customerDiscountService = customerDiscountService;
        _taxConfigurationService = taxConfigurationService;
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

                    order.Items.Add(orderItem);
                    subTotal += orderItem.ItemTotal;
                }
            }

            // REFACTORED TAX FLOW: Tax is extracted from item prices for display only
            // It does NOT affect the final customer payment
            // Example: Product 16.90 → Tax extracted 0.44 → SubTotal shown 16.46 → Customer pays 16.90

            // itemsTotal = sum of all item prices (what customer pays for items)
            decimal itemsTotal = subTotal;

            // Calculate tax on items total - this is for display/bills only
            order.Tax = await CalculateTax(itemsTotal, command.Type, cancellationToken);

            // SubTotal = items total minus the extracted tax (for display purposes)
            order.SubTotal = itemsTotal - order.Tax;

            // DeliveryFee is added to final price
            order.DeliveryFee = command.Type == OrderType.Delivery ? CalculateDeliveryFee() : 0;

            // Apply user discount (calculated on items total, before tax extraction)
            if (command.HasUserLimitDiscount && itemsTotal >= command.UserLimitAmount)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null && user.IsDiscountActive)
                {
                    order.DiscountPercentage = user.DiscountPercentage;
                    // Discount applies to items total (before tax extraction)
                    order.Discount = itemsTotal * (user.DiscountPercentage / 100);
                }
            }

            // Apply customer-specific discount if available
            if (userId.HasValue)
            {
                var customerDiscount = await _customerDiscountService.FindBestApplicableDiscountAsync(
                    userId.Value,
                    itemsTotal,
                    cancellationToken);

                if (customerDiscount != null)
                {
                    // Discount calculated on items total (before tax extraction)
                    var discountAmount = _customerDiscountService.CalculateDiscountAmount(customerDiscount, itemsTotal);
                    order.CustomerDiscountAmount = discountAmount;
                    order.CustomerDiscountRuleId = customerDiscount.Id;

                    // Apply the discount and increment usage count
                    await _customerDiscountService.ApplyDiscountAsync(customerDiscount.Id, cancellationToken);

                    _logger.LogInformation("Applied customer discount {DiscountName} of ${Amount} to order",
                        customerDiscount.Name, discountAmount);
                }
            }

            // Handle fidelity points redemption (if requested)
            // Note: This would come from command.PointsToRedeem in a future update
            // For now, we'll just calculate points to earn

            // Calculate total: Items + DeliveryFee - Discounts - FidelityDiscount
            // NOTE: Tax is NOT added to total - it's extracted from items and shown for display only
            var totalBeforeFidelity = itemsTotal + order.DeliveryFee - order.Discount - order.CustomerDiscountAmount;

            // Calculate fidelity points to earn for this order
            if (userId.HasValue)
            {
                var pointsToEarn = await _fidelityPointsService.CalculatePointsForOrderAsync(itemsTotal, cancellationToken);
                order.FidelityPointsEarned = pointsToEarn;

                _logger.LogInformation("Order will earn {Points} fidelity points", pointsToEarn);
            }

            // Calculate total and apply special rounding for discounted customers
            var calculatedTotal = totalBeforeFidelity - order.FidelityPointsDiscount;
            bool hasActiveDiscount = PriceRoundingUtility.HasActiveDiscount(order.CustomerDiscountAmount + order.Discount);
            order.Total = PriceRoundingUtility.ApplySpecialRounding(calculatedTotal, hasActiveDiscount);

            // Process payments
            foreach (var paymentDto in command.Payments)
            {
                var payment = new OrderPayment
                {
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

                // Only mark non-Cash payments as completed immediately
                // Cash payments remain Pending until explicitly completed via AddPaymentToOrder
                if (payment.PaymentMethod != PaymentMethod.Cash)
                {
                    payment.Status = PaymentStatus.Completed;
                    // Here you would integrate with payment gateways
                }

                order.Payments.Add(payment);
            }

            // Calculate totalPaid from only completed payments
            // Cash payments created with Pending status should not count until explicitly completed
            decimal totalPaid = order.Payments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);

            // Update payment summary
            order.TotalPaid = totalPaid;
            order.RemainingAmount = order.Total - totalPaid;

            // Update payment status with tolerance for floating point precision in financial calculations
            // Use 0.01 (1 cent) as tolerance to handle rounding errors
            const decimal tolerance = 0.01m;
            if (order.RemainingAmount <= tolerance)
            {
                // If remaining is within tolerance of zero or negative, it's fully paid or overpaid
                order.PaymentStatus = order.RemainingAmount < -tolerance ? PaymentStatus.Overpaid : PaymentStatus.Completed;
            }
            else if (totalPaid > 0)
            {
                order.PaymentStatus = PaymentStatus.PartiallyPaid;
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Pending;
            }

            // Add initial status history
            // Add order status history
        var statusHistory = new OrderStatusHistory
        {
            FromStatus = OrderStatus.Pending,
            ToStatus = order.Status,
            Notes = "Order created",
            ChangedAt = DateTime.UtcNow,
            ChangedBy = _currentUserService.UserId?.ToString() ?? "System",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
        };

        order.StatusHistory.Add(statusHistory);

            // Calculate estimated delivery time
            if (command.Type == OrderType.Delivery)
            {
                order.EstimatedDeliveryTime = DateTime.UtcNow.AddMinutes(45); // Example: 45 minutes
            }

            await _context.SaveChangesAsync(cancellationToken);
            
            // Award fidelity points after successful order creation
            if (userId.HasValue && order.FidelityPointsEarned > 0)
            {
                try
                {
                    await _fidelityPointsService.AwardPointsAsync(
                        userId.Value, 
                        order.Id, 
                        order.FidelityPointsEarned, 
                        order.SubTotal, 
                        cancellationToken);
                    
                    _logger.LogInformation("Awarded {Points} fidelity points to user {UserId} for order {OrderNumber}",
                        order.FidelityPointsEarned, userId, order.OrderNumber);
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the order
                    _logger.LogError(ex, "Failed to award fidelity points for order {OrderNumber}, but order was created successfully", 
                        order.OrderNumber);
                }
            }
            
            await transaction.CommitAsync(cancellationToken);

            // Map to DTO
            var orderDto = await _mappingService.MapToOrderDtoAsync(order, cancellationToken);

            _logger.LogInformation("Sending kitchen notification for order {OrderNumber}", order.OrderNumber);
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

    private async Task<decimal> CalculateTax(decimal subTotal, OrderType orderType, CancellationToken cancellationToken)
    {
        return await _taxConfigurationService.CalculateTaxByOrderTypeAsync(subTotal, orderType, cancellationToken);
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
