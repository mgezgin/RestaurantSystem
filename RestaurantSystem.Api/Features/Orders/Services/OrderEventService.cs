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
        var clientsByType = _clients.Values.GroupBy(c => c.ClientType).ToDictionary(g => g.Key, g => g.Count());
        _logger.LogInformation("SSE client {ClientId} ({ClientType}) connected. Total clients: {Count} (Kitchen: {Kitchen}, Service: {Service}, Manager: {Manager}, Stock: {Stock})",
            clientId, client.ClientType, _clients.Count,
            clientsByType.GetValueOrDefault(ClientType.Kitchen, 0),
            clientsByType.GetValueOrDefault(ClientType.Service, 0),
            clientsByType.GetValueOrDefault(ClientType.Manager, 0),
            clientsByType.GetValueOrDefault(ClientType.Stock, 0));
    }

    public void RemoveClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var removedClient))
        {
            var clientsByType = _clients.Values.GroupBy(c => c.ClientType).ToDictionary(g => g.Key, g => g.Count());
            _logger.LogInformation("SSE client {ClientId} ({ClientType}) disconnected. Total clients: {Count} (Kitchen: {Kitchen}, Service: {Service}, Manager: {Manager}, Stock: {Stock})",
                clientId, removedClient.ClientType, _clients.Count,
                clientsByType.GetValueOrDefault(ClientType.Kitchen, 0),
                clientsByType.GetValueOrDefault(ClientType.Service, 0),
                clientsByType.GetValueOrDefault(ClientType.Manager, 0),
                clientsByType.GetValueOrDefault(ClientType.Stock, 0));
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
        var managerClients = _clients.Values.Count(c => c.ClientType == ClientType.Manager);
        var allClients = _clients.Count;

        _logger.LogInformation("=== ORDER CREATED: {OrderNumber} ===", order.OrderNumber);
        _logger.LogInformation("Total connected clients: {TotalClients} (Kitchen: {Kitchen}, Service: {Service}, Manager: {Manager})",
            allClients, kitchenClients, serviceClients, managerClients);
        _logger.LogInformation("Will notify {KitchenCount} kitchen, {ServiceCount} service, and {ManagerCount} manager client(s)",
            kitchenClients, serviceClients, managerClients);

        // Notify kitchen staff of new orders
        await SendEventToClients(eventData, ClientType.Kitchen);

        // Also notify service staff (cashiers) of new orders
        await SendEventToClients(eventData, ClientType.Service);

        _logger.LogInformation("=== Order creation notification completed for order {OrderNumber} ===", order.OrderNumber);
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

        // Create snapshot to avoid race conditions during iteration
        var targetClients = _clients.Values.Where(c =>
            targetClientType == ClientType.All || c.ClientType == targetClientType || c.ClientType == ClientType.Manager).ToList();

        _logger.LogInformation("Broadcasting event {EventType} to {ClientCount} {ClientType} client(s): [{ClientIds}]",
            eventData.EventType, targetClients.Count, targetClientType,
            string.Join(", ", targetClients.Select(c => c.ClientId)));

        if (targetClients.Count == 0)
        {
            _logger.LogWarning("No clients to broadcast event {EventType} for type {ClientType}",
                eventData.EventType, targetClientType);
            return;
        }

        // Send to all clients in parallel, but track each individually
        int successCount = 0;
        int failureCount = 0;
        var sendTasks = targetClients.Select(async client =>
        {
            try
            {
                await SendToClient(client, eventBytes);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                _logger.LogError(ex, "Unhandled exception in SendToClient for {ClientId}", client.ClientId);
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);

        _logger.LogInformation("Event {EventType} broadcast completed: {SuccessCount} succeeded, {FailureCount} failed out of {TotalCount} clients",
            eventData.EventType, successCount, failureCount, targetClients.Count);
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

        _logger.LogInformation("Broadcasting event {EventType} to {ClientCount} {ClientType} client(s): [{ClientIds}]",
            eventData.EventType, targetClients.Count, targetClientType,
            string.Join(", ", targetClients.Select(c => c.ClientId)));

        if (targetClients.Count == 0)
        {
            _logger.LogWarning("No clients to broadcast event {EventType} for type {ClientType}",
                eventData.EventType, targetClientType);
            return;
        }

        // Send to all clients in parallel, but track each individually
        int successCount = 0;
        int failureCount = 0;
        var sendTasks = targetClients.Select(async client =>
        {
            try
            {
                await SendToClient(client, eventBytes);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                _logger.LogError(ex, "Unhandled exception in SendToClient for {ClientId}", client.ClientId);
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);

        _logger.LogInformation("Event {EventType} broadcast completed: {SuccessCount} succeeded, {FailureCount} failed out of {TotalCount} clients",
            eventData.EventType, successCount, failureCount, targetClients.Count);
    }

    private async Task SendToClient(SseClient client, byte[] eventBytes)
    {
        try
        {
            _logger.LogInformation("Sending event to client {ClientId} ({ClientType}), {ByteCount} bytes",
                client.ClientId, client.ClientType, eventBytes.Length);

            // Use timeout to prevent hanging on dead connections (5 seconds max per client)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            // Acquire lock to prevent concurrent writes (heartbeat vs event)
            await client.WriteLock.WaitAsync(cts.Token);
            try
            {
                await client.Response.Body.WriteAsync(eventBytes, cts.Token);
                await client.Response.Body.FlushAsync(cts.Token);
            }
            finally
            {
                client.WriteLock.Release();
            }

            _logger.LogInformation("✓ Event successfully sent to client {ClientId}", client.ClientId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("✗ Timeout sending event to client {ClientId} - removing client", client.ClientId);
            RemoveClient(client.ClientId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✗ Failed to send event to client {ClientId} - removing client", client.ClientId);
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

    public object GetClientStatistics()
    {
        var clientsByType = _clients.Values.GroupBy(c => c.ClientType)
            .ToDictionary(g => g.Key.ToString(), g => g.Select(c => new
            {
                clientId = c.ClientId,
                connectedAt = c.ConnectedAt,
                connectedDuration = DateTime.UtcNow - c.ConnectedAt
            }).ToList());

        return new
        {
            totalClients = _clients.Count,
            kitchenClients = _clients.Values.Count(c => c.ClientType == ClientType.Kitchen),
            serviceClients = _clients.Values.Count(c => c.ClientType == ClientType.Service),
            managerClients = _clients.Values.Count(c => c.ClientType == ClientType.Manager),
            stockClients = _clients.Values.Count(c => c.ClientType == ClientType.Stock),
            clientDetails = clientsByType,
            timestamp = DateTime.UtcNow
        };
    }

    public class SseClient
    {
        public string ClientId { get; set; } = null!;
        public HttpResponse Response { get; set; } = null!;
        public ClientType ClientType { get; set; }
        public DateTime ConnectedAt { get; set; }

        // Synchronization for concurrent writes (heartbeats vs events)
        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);
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
