using RestaurantSystem.Api.Features.Orders.Dtos;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace RestaurantSystem.Api.Features.Orders.Services;

public class OrderEventService : IOrderEventService
{
    private readonly ConcurrentDictionary<string, SseClient> _clients = new();
    private readonly ILogger<OrderEventService> _logger;
    private readonly ConcurrentQueue<LogEntry> _recentLogs = new();
    private const int MaxLogEntries = 100;

    public OrderEventService(ILogger<OrderEventService> logger)
    {
        _logger = logger;
    }

    private void AddLog(string level, string message, string? eventType = null, string? clientId = null)
    {
        _recentLogs.Enqueue(new LogEntry
        {
            Timestamp = DateTime.UtcNow,
            Level = level,
            Message = message,
            EventType = eventType,
            ClientId = clientId
        });

        // Keep only last MaxLogEntries
        while (_recentLogs.Count > MaxLogEntries)
        {
            _recentLogs.TryDequeue(out _);
        }
    }

    public void AddClient(string clientId, SseClient client)
    {
        _clients.TryAdd(clientId, client);
        var clientsByType = _clients.Values.GroupBy(c => c.ClientType).ToDictionary(g => g.Key, g => g.Count());
        var message = $"SSE client {clientId} ({client.ClientType}) connected from {client.IpAddress} ({client.Country ?? "Unknown"}). Total clients: {_clients.Count} (Kitchen: {clientsByType.GetValueOrDefault(ClientType.Kitchen, 0)}, Service: {clientsByType.GetValueOrDefault(ClientType.Service, 0)}, Manager: {clientsByType.GetValueOrDefault(ClientType.Manager, 0)}, Stock: {clientsByType.GetValueOrDefault(ClientType.Stock, 0)})";

        _logger.LogInformation(message);
        AddLog("Info", message, null, clientId);
    }

    public void RemoveClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var removedClient))
        {
            var clientsByType = _clients.Values.GroupBy(c => c.ClientType).ToDictionary(g => g.Key, g => g.Count());
            var message = $"SSE client {clientId} ({removedClient.ClientType}) disconnected. Total clients: {_clients.Count} (Kitchen: {clientsByType.GetValueOrDefault(ClientType.Kitchen, 0)}, Service: {clientsByType.GetValueOrDefault(ClientType.Service, 0)}, Manager: {clientsByType.GetValueOrDefault(ClientType.Manager, 0)}, Stock: {clientsByType.GetValueOrDefault(ClientType.Stock, 0)})";

            _logger.LogInformation(message);
            AddLog("Info", message, null, clientId);
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

        var msg1 = $"=== ORDER CREATED: {order.OrderNumber} ===";
        var msg2 = $"Total connected clients: {allClients} (Kitchen: {kitchenClients}, Service: {serviceClients}, Manager: {managerClients})";
        var msg3 = $"Will notify {kitchenClients} kitchen, {serviceClients} service, and {managerClients} manager client(s)";

        _logger.LogInformation(msg1);
        _logger.LogInformation(msg2);
        _logger.LogInformation(msg3);

        AddLog("Info", msg1, "order-created");
        AddLog("Info", msg2, "order-created");
        AddLog("Info", msg3, "order-created");

        // Notify kitchen staff of new orders
        await SendEventToClients(eventData, ClientType.Kitchen);

        // Also notify service staff (cashiers) of new orders
        await SendEventToClients(eventData, ClientType.Service);

        var msg4 = $"=== Order creation notification completed for order {order.OrderNumber} ===";
        _logger.LogInformation(msg4);
        AddLog("Info", msg4, "order-created");
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

        var broadcastMsg = $"Broadcasting event {eventData.EventType} to {targetClients.Count} {targetClientType} client(s): [{string.Join(", ", targetClients.Select(c => c.ClientId))}]";
        _logger.LogInformation(broadcastMsg);
        AddLog("Info", broadcastMsg, eventData.EventType);

        if (targetClients.Count == 0)
        {
            var warnMsg = $"No clients to broadcast event {eventData.EventType} for type {targetClientType}";
            _logger.LogWarning(warnMsg);
            AddLog("Warning", warnMsg, eventData.EventType);
            return;
        }

        // Send to all clients in parallel, but track each individually
        int successCount = 0;
        int failureCount = 0;
        var sendTasks = targetClients.Select(async client =>
        {
            try
            {
                await SendToClient(client, eventBytes, eventData.EventType);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                var errorMsg = $"Unhandled exception in SendToClient for {client.ClientId}";
                _logger.LogError(ex, errorMsg);
                AddLog("Error", $"{errorMsg}: {ex.Message}", eventData.EventType, client.ClientId);
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);

        var completeMsg = $"Event {eventData.EventType} broadcast completed: {successCount} succeeded, {failureCount} failed out of {targetClients.Count} clients";
        _logger.LogInformation(completeMsg);
        AddLog("Info", completeMsg, eventData.EventType);
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

        var broadcastMsg = $"Broadcasting event {eventData.EventType} to {targetClients.Count} {targetClientType} client(s): [{string.Join(", ", targetClients.Select(c => c.ClientId))}]";
        _logger.LogInformation(broadcastMsg);
        AddLog("Info", broadcastMsg, eventData.EventType);

        if (targetClients.Count == 0)
        {
            var warnMsg = $"No clients to broadcast event {eventData.EventType} for type {targetClientType}";
            _logger.LogWarning(warnMsg);
            AddLog("Warning", warnMsg, eventData.EventType);
            return;
        }

        // Send to all clients in parallel, but track each individually
        int successCount = 0;
        int failureCount = 0;
        var sendTasks = targetClients.Select(async client =>
        {
            try
            {
                await SendToClient(client, eventBytes, eventData.EventType);
                Interlocked.Increment(ref successCount);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failureCount);
                var errorMsg = $"Unhandled exception in SendToClient for {client.ClientId}";
                _logger.LogError(ex, errorMsg);
                AddLog("Error", $"{errorMsg}: {ex.Message}", eventData.EventType, client.ClientId);
            }
        }).ToArray();

        await Task.WhenAll(sendTasks);

        var completeMsg = $"Event {eventData.EventType} broadcast completed: {successCount} succeeded, {failureCount} failed out of {targetClients.Count} clients";
        _logger.LogInformation(completeMsg);
        AddLog("Info", completeMsg, eventData.EventType);
    }

    private async Task SendToClient(SseClient client, byte[] eventBytes, string? eventType = null)
    {
        try
        {
            var sendingMsg = $"Sending event to client {client.ClientId} ({client.ClientType}), {eventBytes.Length} bytes";
            _logger.LogInformation(sendingMsg);
            AddLog("Info", sendingMsg, eventType, client.ClientId);

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

            // Track successful send
            client.SuccessfulSends++;
            client.LastEventSentAt = DateTime.UtcNow;

            var successMsg = $"✓ Event successfully sent to client {client.ClientId}";
            _logger.LogInformation(successMsg);
            AddLog("Info", successMsg, eventType, client.ClientId);
        }
        catch (OperationCanceledException)
        {
            var timeoutMsg = $"✗ Timeout sending event to client {client.ClientId} - removing client";
            _logger.LogWarning(timeoutMsg);
            AddLog("Warning", timeoutMsg, eventType, client.ClientId);

            // Track error
            client.FailedSends++;
            client.Errors.Add(new ClientError
            {
                Timestamp = DateTime.UtcNow,
                ErrorType = "Timeout",
                Message = "Timeout sending event (5 seconds exceeded)",
                EventType = eventType
            });

            RemoveClient(client.ClientId);
        }
        catch (Exception ex)
        {
            var errorMsg = $"✗ Failed to send event to client {client.ClientId} - removing client: {ex.Message}";
            _logger.LogError(ex, "✗ Failed to send event to client {ClientId} - removing client", client.ClientId);
            AddLog("Error", errorMsg, eventType, client.ClientId);

            // Track error
            client.FailedSends++;
            client.Errors.Add(new ClientError
            {
                Timestamp = DateTime.UtcNow,
                ErrorType = ex.GetType().Name,
                Message = ex.Message,
                EventType = eventType
            });

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
                ipAddress = c.IpAddress,
                country = c.Country ?? "Unknown",
                connectedAt = c.ConnectedAt,
                connectedDuration = DateTime.UtcNow - c.ConnectedAt,
                successfulSends = c.SuccessfulSends,
                failedSends = c.FailedSends,
                lastEventSentAt = c.LastEventSentAt,
                errors = c.Errors.Select(e => new
                {
                    timestamp = e.Timestamp,
                    errorType = e.ErrorType,
                    message = e.Message,
                    eventType = e.EventType
                }).ToList(),
                hasErrors = c.Errors.Any(),
                errorCount = c.Errors.Count
            }).ToList());

        var allErrors = _clients.Values
            .SelectMany(c => c.Errors.Select(e => new
            {
                clientId = c.ClientId,
                clientType = c.ClientType.ToString(),
                ipAddress = c.IpAddress,
                country = c.Country ?? "Unknown",
                timestamp = e.Timestamp,
                errorType = e.ErrorType,
                message = e.Message,
                eventType = e.EventType
            }))
            .OrderByDescending(e => e.timestamp)
            .ToList();

        var recentLogs = _recentLogs.OrderByDescending(l => l.Timestamp).Take(50).Select(l => new
        {
            timestamp = l.Timestamp,
            level = l.Level,
            message = l.Message,
            eventType = l.EventType,
            clientId = l.ClientId
        }).ToList();

        return new
        {
            totalClients = _clients.Count,
            kitchenClients = _clients.Values.Count(c => c.ClientType == ClientType.Kitchen),
            serviceClients = _clients.Values.Count(c => c.ClientType == ClientType.Service),
            managerClients = _clients.Values.Count(c => c.ClientType == ClientType.Manager),
            stockClients = _clients.Values.Count(c => c.ClientType == ClientType.Stock),
            clientsWithErrors = _clients.Values.Count(c => c.Errors.Any()),
            totalErrors = _clients.Values.Sum(c => c.Errors.Count),
            totalSuccessfulSends = _clients.Values.Sum(c => c.SuccessfulSends),
            totalFailedSends = _clients.Values.Sum(c => c.FailedSends),
            clientDetails = clientsByType,
            recentErrors = allErrors.Take(20).ToList(), // Last 20 errors across all clients
            recentLogs = recentLogs, // Last 50 log entries
            timestamp = DateTime.UtcNow
        };
    }

    public class SseClient
    {
        public string ClientId { get; set; } = null!;
        public HttpResponse Response { get; set; } = null!;
        public ClientType ClientType { get; set; }
        public DateTime ConnectedAt { get; set; }
        public string IpAddress { get; set; } = null!;
        public string? Country { get; set; }

        // Synchronization for concurrent writes (heartbeats vs events)
        public SemaphoreSlim WriteLock { get; } = new SemaphoreSlim(1, 1);

        // Error tracking
        public List<ClientError> Errors { get; } = new List<ClientError>();
        public int SuccessfulSends { get; set; } = 0;
        public int FailedSends { get; set; } = 0;
        public DateTime? LastEventSentAt { get; set; }
    }

    public class ClientError
    {
        public DateTime Timestamp { get; set; }
        public string ErrorType { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? EventType { get; set; }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? EventType { get; set; }
        public string? ClientId { get; set; }
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
