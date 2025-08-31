using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Orders.Commands.CreateOrderCommand;

public record CreateOrderCommand : ICommand<ApiResponse<OrderDto>>
{
    public Guid? UserId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }

    // Order Type
    public OrderType Type { get; set; }
    public int? TableNumber { get; set; }

    // Discount
    public string? PromoCode { get; set; }
    public bool HasUserLimitDiscount { get; set; }
    public decimal UserLimitAmount { get; set; }

    // Focus Order
    public bool IsFocusOrder { get; set; }
    public int? Priority { get; set; }
    public string? FocusReason { get; set; }

    // Additional Info
    public string? Notes { get; set; }

    public CreateOrderDeliveryAddressDto? DeliveryAddress { get; set; }

    // Order Items
    public List<CreateOrderItemDto> Items { get; set; } = new();

    // Multiple Payments
    public List<CreateOrderPaymentDto> Payments { get; set; } = new();
}

