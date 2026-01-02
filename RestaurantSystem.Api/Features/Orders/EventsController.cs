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

    /// <summary>
    /// Get diagnostic info about connected clients
    /// </summary>
    [HttpGet("diagnostics")]
    public IActionResult GetDiagnostics()
    {
        var stats = _orderEventService.GetClientStatistics();
        return Ok(stats);
    }

    private async Task SetupSseConnection(OrderEventService.ClientType clientType, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid().ToString();

        // Set response headers BEFORE writing any data
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no"; // Disable Nginx buffering
        Response.Headers["Access-Control-Allow-Origin"] = "*";

        // Disable ASP.NET response buffering for real-time streaming
        var bufferingFeature = Response.HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
        bufferingFeature?.DisableBuffering();

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

            // Send initial connection event with lock
            await client.WriteLock.WaitAsync(cancellationToken);
            try
            {
                await Response.WriteAsync($"event: connected\ndata: {connectionJson}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            finally
            {
                client.WriteLock.Release();
            }

            // Keep connection alive with heartbeats (every 15 seconds for better reliability)
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(15000, cancellationToken);

                // Send proper SSE event format (not just a comment) so frontend can track heartbeats
                var heartbeatData = new { timestamp = DateTime.UtcNow };
                var heartbeatJson = System.Text.Json.JsonSerializer.Serialize(heartbeatData,
                    new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });

                // Acquire lock to prevent concurrent writes with events
                await client.WriteLock.WaitAsync(cancellationToken);
                try
                {
                    await Response.WriteAsync($"event: heartbeat\ndata: {heartbeatJson}\n\n", cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);
                }
                finally
                {
                    client.WriteLock.Release();
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Normal disconnect - client closed connection or server shutting down
            _logger.LogInformation("SSE client disconnected normally: {ClientId}", clientId);
        }
        catch (OperationCanceledException)
        {
            // Also normal for connection close
            _logger.LogInformation("SSE client connection closed: {ClientId}", clientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SSE connection error for client {ClientId}", clientId);
        }
        finally
        {
            _logger.LogInformation("SSE client cleanup: {ClientId}", clientId);
            _orderEventService.RemoveClient(clientId);
        }
    }
}
