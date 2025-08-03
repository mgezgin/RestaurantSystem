using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Features.Orders.Services;

namespace RestaurantSystem.Api.Features.Orders;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly OrderEventService _orderEventService;
    private readonly ILogger<EventsController> _logger;

    public EventsController(OrderEventService orderEventService, ILogger<EventsController> logger)
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

        Response.Headers.Add("Content-Type", "text/event-stream");
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no"); // Disable Nginx buffering

        var client = new OrderEventService.SseClient
        {
            ClientId = clientId,
            Response = Response,
            ClientType = clientType,
            ConnectedAt = DateTime.UtcNow
        };

        _orderEventService.AddClient(clientId, client);

        try
        {
            // Send initial connection event
            await Response.WriteAsync($"event: connected\ndata: {{\"clientId\":\"{clientId}\",\"clientType\":\"{clientType}\"}}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Keep connection alive
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
            _orderEventService.RemoveClient(clientId);
        }
    }
}
