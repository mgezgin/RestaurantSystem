using RestaurantSystem.Api.Features.Orders.Dtos;

namespace RestaurantSystem.Api.Features.Orders.Services;

public interface IOrderEventService
{
    Task NotifyOrderCreated(OrderDto order);
    Task NotifyOrderStatusChanged(OrderDto order, string previousStatus);
    Task NotifyOrderReady(OrderDto order);
    Task NotifyOrderCompleted(OrderDto order);
    Task NotifyFocusOrderUpdate(OrderDto order);
}
