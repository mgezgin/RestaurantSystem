using RestaurantSystem.Api.Features.Orders.Dtos;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace RestaurantSystem.Api.Features.Orders.Services;

public class OrderEventService : IOrderEventService
{
    private readonly ConcurrentDictionary<string, SseClient> _clients = new();
    private readonly ILogger<OrderEventService> _logger;

    public OrderEventService(ILogger<OrderEventService> logger)
    {
        _logger = logger;
    }

    public void AddClient(string clientId, SseClient client)
    {
        _clients.TryAdd(clientId, client);
        _logger.LogInformation("SSE client {ClientId} connected. Total clients: {Count}", clientId, _clients.Count);
    }

    public void RemoveClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out _))
        {
            _logger.LogInformation("SSE client {ClientId} disconnected. Total clients: {Count}", clientId, _clients.Count);
        }
    }

    public async Task NotifyStockUpdate(string stock)
    {
        var eventData = new StockEvent
        {
            EventType = "stock-updated",
            PreviousStatus = stock,
            Timestamp = DateTime.UtcNow
        };
        // Notify all staff about the updated order
        await SendEventToClients(eventData, ClientType.All);
    }

    public async Task NotifyOrderCreated(OrderDto order)
    {
        var eventData = new OrderEvent
        {
            EventType = "order-created",
            Order = order,
            Timestamp = DateTime.UtcNow
        };

        var kitchenClients = _clients.Values.Count(c => c.ClientType == ClientType.Kitchen);
        var serviceClients = _clients.Values.Count(c => c.ClientType == ClientType.Service);
        _logger.LogInformation("Notifying {KitchenCount} kitchen and {ServiceCount} service client(s) of new order {OrderNumber}",
            kitchenClients, serviceClients, order.OrderNumber);

        // Notify kitchen staff of new orders
        await SendEventToClients(eventData, ClientType.Kitchen);

        // Also notify service staff (cashiers) of new orders
        await SendEventToClients(eventData, ClientType.Service);

        _logger.LogInformation("Order creation notification sent for order {OrderNumber}", order.OrderNumber);
    }

    public async Task NotifyOrderStatusChanged(OrderDto order, string previousStatus)
    {
        var eventData = new OrderEvent
        {
            EventType = "order-status-changed",
            Order = order,
            PreviousStatus = previousStatus,
            Timestamp = DateTime.UtcNow
        };

        // Determine which clients to notify based on status
        var clientTypes = GetClientTypesForStatus(order.Status);

        foreach (var clientType in clientTypes)
        {
            await SendEventToClients(eventData, clientType);
        }

        _logger.LogInformation("Notified clients of order {OrderNumber} status change from {Previous} to {Current}",
            order.OrderNumber, previousStatus, order.Status);
    }

    public async Task NotifyOrderReady(OrderDto order)
    {
        var eventData = new OrderEvent
        {
            EventType = "order-ready",
            Order = order,
            Timestamp = DateTime.UtcNow
        };

        // Notify service staff that order is ready
        await SendEventToClients(eventData, ClientType.Service);

        _logger.LogInformation("Notified service staff that order {OrderNumber} is ready", order.OrderNumber);
    }

    public async Task NotifyOrderCompleted(OrderDto order)
    {
        var eventData = new OrderEvent
        {
            EventType = "order-completed",
            Order = order,
            Timestamp = DateTime.UtcNow
        };

        // Notify all staff that order is completed
        await SendEventToClients(eventData, ClientType.All);

        _logger.LogInformation("Notified all staff that order {OrderNumber} is completed", order.OrderNumber);
    }

    public async Task NotifyFocusOrderUpdate(OrderDto order)
    {
        var eventData = new OrderEvent
        {
            EventType = "focus-order-update",
            Order = order,
            Timestamp = DateTime.UtcNow
        };

        // Notify all relevant staff about focus order updates
        await SendEventToClients(eventData, ClientType.All);

        _logger.LogInformation("Notified staff about focus order update for {OrderNumber}", order.OrderNumber);
    }

    private async Task SendEventToClients(StockEvent eventData, ClientType targetClientType)
    {
        var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventMessage = $"event: {eventData.EventType}\ndata: {json}\n\n";
        var eventBytes = Encoding.UTF8.GetBytes(eventMessage);

        var tasks = new List<Task>();

        foreach (var client in _clients.Values.Where(c =>
            targetClientType == ClientType.All || c.ClientType == targetClientType || c.ClientType == ClientType.Manager))
        {
            tasks.Add(SendToClient(client, eventBytes));
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendEventToClients(OrderEvent eventData, ClientType targetClientType)
    {
        var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventMessage = $"event: {eventData.EventType}\ndata: {json}\n\n";
        var eventBytes = Encoding.UTF8.GetBytes(eventMessage);

        var targetClients = _clients.Values.Where(c =>
            targetClientType == ClientType.All || c.ClientType == targetClientType || c.ClientType == ClientType.Manager).ToList();

        _logger.LogInformation("Broadcasting event {EventType} to {ClientCount} {ClientType} client(s)",
            eventData.EventType, targetClients.Count, targetClientType);

        var tasks = new List<Task>();

        foreach (var client in targetClients)
        {
            tasks.Add(SendToClient(client, eventBytes));
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
    }

    private async Task SendToClient(SseClient client, byte[] eventBytes)
    {
        try
        {
            _logger.LogDebug("Sending event to client {ClientId} ({ClientType}), {ByteCount} bytes",
                client.ClientId, client.ClientType, eventBytes.Length);

            await client.Response.Body.WriteAsync(eventBytes);
            await client.Response.Body.FlushAsync();

            _logger.LogDebug("Event successfully sent to client {ClientId}", client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event to client {ClientId}", client.ClientId);
            RemoveClient(client.ClientId);
        }
    }

    private List<ClientType> GetClientTypesForStatus(string status)
    {
        // Service (cashiers) should always be notified of status changes
        // Kitchen should be notified for statuses they care about
        return status switch
        {
            "Pending" or "PendingApproval" => new List<ClientType> { ClientType.Kitchen, ClientType.Service },
            "Confirmed" or "Preparing" => new List<ClientType> { ClientType.Kitchen, ClientType.Service },
            "Ready" => new List<ClientType> { ClientType.Kitchen, ClientType.Service },
            "OutForDelivery" or "Completed" or "Cancelled" => new List<ClientType> { ClientType.All },
            _ => new List<ClientType> { ClientType.All }
        };
    }

    public class SseClient
    {
        public string ClientId { get; set; } = null!;
        public HttpResponse Response { get; set; } = null!;
        public ClientType ClientType { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    public class OrderEvent
    {
        public string EventType { get; set; } = null!;
        public OrderDto Order { get; set; } = null!;
        public string? PreviousStatus { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class StockEvent
    {
        public string EventType { get; set; } = null!;
        public string? PreviousStatus { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public enum ClientType
    {
        Kitchen,
        Service,
        Manager,
        Stock,
        All
    }
}
