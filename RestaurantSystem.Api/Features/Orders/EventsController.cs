using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Features.Orders.Services;

namespace RestaurantSystem.Api.Features.Orders;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly IOrderEventService _orderEventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(IOrderEventService orderEventService, ILogger<EventsController> logger)
    {
        _orderEventService = orderEventService;
        _logger = logger;
    }

    /// <summary>
    /// Subscribe to kitchen order events
    /// </summary>
    [HttpGet("kitchen")]
    [Produces("text/event-stream")]
    public async Task KitchenEvents(CancellationToken cancellationToken)
    {
        await SetupSseConnection(OrderEventService.ClientType.Kitchen, cancellationToken);
    }

    /// <summary>
    /// Subscribe to kitchen order events
    /// </summary>
    [HttpGet("stock")]
    [Produces("text/event-stream")]
    public async Task StockEvents(CancellationToken cancellationToken)
    {
        await SetupSseConnection(OrderEventService.ClientType.Stock, cancellationToken);
    }

    /// <summary>
    /// Subscribe to service order events
    /// </summary>
    [HttpGet("service")]
    [Produces("text/event-stream")]
    public async Task ServiceEvents(CancellationToken cancellationToken)
    {
        await SetupSseConnection(OrderEventService.ClientType.Service, cancellationToken);
    }

    /// <summary>
    /// Subscribe to all order events (for managers)
    /// </summary>
    [HttpGet("all")]
    [Produces("text/event-stream")]
    [RequireAdmin]
    public async Task AllEvents(CancellationToken cancellationToken)
    {
        await SetupSseConnection(OrderEventService.ClientType.Manager, cancellationToken);
    }

    private async Task SetupSseConnection(OrderEventService.ClientType clientType, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid().ToString();

        // Set response headers BEFORE writing any data
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Disable Nginx buffering
        Response.Headers["Access-Control-Allow-Origin"] = "*";

        var client = new OrderEventService.SseClient
        {
            ClientId = clientId,
            Response = Response,
            ClientType = clientType,
            ConnectedAt = DateTime.UtcNow
        };

        _orderEventService.AddClient(clientId, client);

        _logger.LogInformation("SSE client connected: {ClientId} with type {ClientType}", clientId, clientType);

        try
        {
            // Send initial connection event
            var connectionData = new
            {
                clientId,
                clientType = clientType.ToString(),
                timestamp = DateTime.UtcNow
            };
            var connectionJson = System.Text.Json.JsonSerializer.Serialize(connectionData,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

            await Response.WriteAsync($"event: connected\ndata: {connectionJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Keep connection alive with heartbeats
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(30000, cancellationToken); // Send heartbeat every 30 seconds
                await Response.WriteAsync(":heartbeat\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE connection error for client {ClientId}", clientId);
        }
        finally
        {
            _logger.LogInformation("SSE client disconnected: {ClientId}", clientId);
            _orderEventService.RemoveClient(clientId);
        }
    }
}
