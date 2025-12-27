using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Features.Orders.Services;

namespace RestaurantSystem.Api.Features.Orders;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IOrderEventService _orderEventService;

    public EventsController(IOrderEventService orderEventService)
    {
        _orderEventService = orderEventService;
    }

    /// <summary>
    /// Subscribe to kitchen order events using native .NET 10 TypedResults.ServerSentEvents
    /// </summary>
    [HttpGet("kitchen")]
    public IResult KitchenEvents(CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(
            _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Kitchen, cancellationToken));
    }

    /// <summary>
    /// Subscribe to stock order events using native .NET 10 TypedResults.ServerSentEvents
    /// </summary>
    [HttpGet("stock")]
    public IResult StockEvents(CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(
            _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Stock, cancellationToken));
    }

    /// <summary>
    /// Subscribe to service order events using native .NET 10 TypedResults.ServerSentEvents
    /// </summary>
    [HttpGet("service")]
    public IResult ServiceEvents(CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(
            _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Service, cancellationToken));
    }

    /// <summary>
    /// Subscribe to all order events (for managers) using native .NET 10 TypedResults.ServerSentEvents
    /// </summary>
    [HttpGet("all")]
    [RequireAdmin]
    public IResult AllEvents(CancellationToken cancellationToken)
    {
        return TypedResults.ServerSentEvents(
            _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Manager, cancellationToken));
    }
}
