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
    /// Subscribe to kitchen order events using native .NET 10 SSE support
    /// </summary>
    [HttpGet("kitchen")]
    [Produces("text/event-stream")]
    public IAsyncEnumerable<string> KitchenEvents(CancellationToken cancellationToken)
    {
        ConfigureSseResponse();
        return _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Kitchen, cancellationToken);
    }

    /// <summary>
    /// Subscribe to stock order events using native .NET 10 SSE support
    /// </summary>
    [HttpGet("stock")]
    [Produces("text/event-stream")]
    public IAsyncEnumerable<string> StockEvents(CancellationToken cancellationToken)
    {
        ConfigureSseResponse();
        return _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Stock, cancellationToken);
    }

    /// <summary>
    /// Subscribe to service order events using native .NET 10 SSE support
    /// </summary>
    [HttpGet("service")]
    [Produces("text/event-stream")]
    public IAsyncEnumerable<string> ServiceEvents(CancellationToken cancellationToken)
    {
        ConfigureSseResponse();
        return _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Service, cancellationToken);
    }

    /// <summary>
    /// Subscribe to all order events (for managers) using native .NET 10 SSE support
    /// </summary>
    [HttpGet("all")]
    [Produces("text/event-stream")]
    [RequireAdmin]
    public IAsyncEnumerable<string> AllEvents(CancellationToken cancellationToken)
    {
        ConfigureSseResponse();
        return _orderEventService.SubscribeToEvents(OrderEventService.ClientType.Manager, cancellationToken);
    }

    private void ConfigureSseResponse()
    {
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Disable Nginx buffering
    }
}
