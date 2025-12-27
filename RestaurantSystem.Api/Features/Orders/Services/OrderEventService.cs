using RestaurantSystem.Api.Features.Orders.Dtos;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading.Channels;

namespace RestaurantSystem.Api.Features.Orders.Services;

public class OrderEventService : IOrderEventService
{
    private readonly ConcurrentDictionary<string, ClientSubscription> _clients = new();
    private readonly ILogger<OrderEventService> _logger;

    public OrderEventService(ILogger<OrderEventService> logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<string> SubscribeToEvents(
        ClientType clientType,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid().ToString();
        var channel = Channel.CreateUnbounded<string>();

        var subscription = new ClientSubscription
        {
            ClientId = clientId,
            ClientType = clientType,
            Channel = channel,
            ConnectedAt = DateTime.UtcNow
        };

        _clients.TryAdd(clientId, subscription);
        _logger.LogInformation("SSE client {ClientId} connected with type {ClientType}. Total clients: {Count}",
            clientId, clientType, _clients.Count);

        try
        {
            // Send initial connection event
            var connectionData = new
            {
                clientId,
                clientType = clientType.ToString(),
                timestamp = DateTime.UtcNow
            };
            var connectionJson = JsonSerializer.Serialize(connectionData,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            yield return $"event: connected\ndata: {connectionJson}\n";

            // Read events from the channel
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return message;
            }
        }
        finally
        {
            if (_clients.TryRemove(clientId, out _))
            {
                channel.Writer.Complete();
                _logger.LogInformation("SSE client {ClientId} disconnected. Total clients: {Count}",
                    clientId, _clients.Count);
            }
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

    private Task SendEventToClients(StockEvent eventData, ClientType targetClientType)
    {
        var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventMessage = $"event: {eventData.EventType}\ndata: {json}\n";

        var targetClients = _clients.Values
            .Where(c => targetClientType == ClientType.All || c.ClientType == targetClientType)
            .ToList();

        _logger.LogInformation("Broadcasting event {EventType} to {ClientCount} {ClientType} client(s)",
            eventData.EventType, targetClients.Count, targetClientType);

        foreach (var client in targetClients)
        {
            SendToClient(client, eventMessage);
        }

        return Task.CompletedTask;
    }

    private Task SendEventToClients(OrderEvent eventData, ClientType targetClientType)
    {
        var json = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var eventMessage = $"event: {eventData.EventType}\ndata: {json}\n";

        var targetClients = _clients.Values
            .Where(c => targetClientType == ClientType.All || c.ClientType == targetClientType)
            .ToList();

        _logger.LogInformation("Broadcasting event {EventType} to {ClientCount} {ClientType} client(s)",
            eventData.EventType, targetClients.Count, targetClientType);

        foreach (var client in targetClients)
        {
            SendToClient(client, eventMessage);
        }

        return Task.CompletedTask;
    }

    private void SendToClient(ClientSubscription client, string eventMessage)
    {
        try
        {
            _logger.LogDebug("Sending event to client {ClientId} ({ClientType})",
                client.ClientId, client.ClientType);

            // Non-blocking write to channel
            if (!client.Channel.Writer.TryWrite(eventMessage))
            {
                _logger.LogWarning("Failed to write to channel for client {ClientId}, channel may be full or closed",
                    client.ClientId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send event to client {ClientId}", client.ClientId);
            // Remove the client and complete the channel
            if (_clients.TryRemove(client.ClientId, out _))
            {
                client.Channel.Writer.Complete();
            }
        }
    }

    private List<ClientType> GetClientTypesForStatus(string status)
    {
        return status switch
        {
            "Pending" or "Confirmed" or "Preparing" => new List<ClientType> { ClientType.Kitchen },
            "Ready" => new List<ClientType> { ClientType.Kitchen, ClientType.Service },
            "OutForDelivery" or "Completed" => new List<ClientType> { ClientType.All },
            _ => new List<ClientType> { ClientType.All }
        };
    }

    public class ClientSubscription
    {
        public string ClientId { get; set; } = null!;
        public Channel<string> Channel { get; set; } = null!;
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
