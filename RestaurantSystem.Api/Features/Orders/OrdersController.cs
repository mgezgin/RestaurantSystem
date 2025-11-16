using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Orders.Commands.AddPaymentToOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.CancelOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.CreateOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.RefundPaymentCommand;
using RestaurantSystem.Api.Features.Orders.Commands.ToggleFocusOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.UpdateOrderStatusCommand;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Api.Features.Orders.Queries.GetFocusOrdersQuery;
using RestaurantSystem.Api.Features.Orders.Queries.GetOrderByIdQuery;
using RestaurantSystem.Api.Features.Orders.Queries.GetOrdersQuery;
using RestaurantSystem.Api.Features.Orders.Services;

namespace RestaurantSystem.Api.Features.Orders;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private const string AdminEmail = "rumigeneve@gmail.com";

    private readonly CustomMediator _mediator;
    private readonly IOrderEventService _orderEventService;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(CustomMediator mediator, IOrderEventService orderEventService,
        IEmailService emailService, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _orderEventService = orderEventService;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Get all orders with optional filters
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<OrderDto>>>> GetOrders(
        [FromQuery] GetOrdersQuery query)
    {
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Get all orders with optional filters
    /// </summary>
    [HttpPost("tryEvent")]
    public async Task<ActionResult> GetStocks(
        [FromQuery] string message)
    {
        await _orderEventService.NotifyStockUpdate(message);
        return Ok(message);
    }


    /// <summary>
    /// Get order by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OrderDto>>> GetOrder(Guid id)
    {
        var query = new GetOrderByIdQuery(id);
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new order with multiple payment options
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CreateOrder([FromBody] CreateOrderCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Add a payment to an existing order
    /// </summary>
    [HttpPost("{orderId}/payments")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OrderDto>>> AddPayment(
        Guid orderId,
        [FromBody] AddPaymentToOrderCommand command)
    {
        command.OrderId = orderId;
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Toggle focus order status
    /// </summary>
    [HttpPut("{orderId}/focus")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OrderDto>>> ToggleFocusOrder(
        Guid orderId,
        [FromBody] ToggleFocusOrderCommand command)
    {
        command.OrderId = orderId;
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Get all focus orders
    /// </summary>
    [HttpGet("focus")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<List<OrderDto>>>> GetFocusOrders(
        [FromQuery] GetFocusOrdersQuery query)
    {
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Update order status
    /// </summary>
    [HttpPut("{orderId}/status")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OrderDto>>> UpdateOrderStatus(
        Guid orderId,
        [FromBody] UpdateOrderStatusCommand command)
    {
        command.OrderId = orderId;
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Cancel an order
    /// </summary>
    [HttpPost("{orderId}/cancel")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<OrderDto>>> CancelOrder(
        Guid orderId,
        [FromBody] CancelOrderCommand command)
    {
        command.OrderId = orderId;
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Refund a payment
    /// </summary>
    [HttpPost("{orderId}/payments/{paymentId}/refund")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<OrderPaymentDto>>> RefundPayment(
        Guid orderId,
        Guid paymentId,
        [FromBody] RefundPaymentCommand command)
    {
        command.OrderId = orderId;
        command.PaymentId = paymentId;
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Send order confirmation emails to customer and admin
    /// </summary>
    [HttpPost("{orderId}/send-confirmation-email")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<string>>> SendOrderConfirmationEmail(Guid orderId)
    {
        try
        {
            // Get the order
            var query = new GetOrderByIdQuery(orderId);
            var orderResult = await _mediator.SendQuery(query);

            if (!orderResult.Success || orderResult.Data == null)
            {
                return BadRequest(ApiResponse<string>.Failure("Order not found"));
            }

            var order = orderResult.Data;

            // Prepare order items
            var items = order.Items.Select(item => (
                name: $"{item.ProductName}{(string.IsNullOrEmpty(item.VariationName) ? "" : $" - {item.VariationName}")}",
                quantity: item.Quantity,
                price: item.ItemTotal
            )).ToList();

            // Prepare delivery address if applicable
            string? deliveryAddress = null;
            if (order.DeliveryAddress != null)
            {
                deliveryAddress = $"{order.DeliveryAddress.AddressLine1}, " +
                    $"{order.DeliveryAddress.PostalCode} {order.DeliveryAddress.City}, " +
                    $"{order.DeliveryAddress.Country}";

                if (!string.IsNullOrEmpty(order.DeliveryAddress.DeliveryInstructions))
                {
                    deliveryAddress += $"\n\nDelivery Instructions: {order.DeliveryAddress.DeliveryInstructions}";
                }
            }

            // Send customer confirmation email
            await _emailService.SendOrderConfirmationEmailAsync(
                order.CustomerEmail ?? "noemail@example.com",
                order.CustomerName ?? "Valued Customer",
                order.OrderNumber,
                order.Type.ToString(),
                order.Total,
                items,
                order.Notes,
                deliveryAddress);

            // Send admin notification email (fire and forget - don't block on failure)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendOrderConfirmationAdminEmailAsync(
                        AdminEmail,
                        order.OrderNumber,
                        order.CustomerName ?? "Valued Customer",
                        order.CustomerEmail ?? "noemail@example.com",
                        order.CustomerPhone ?? "Not provided",
                        order.Type.ToString(),
                        order.Total,
                        items,
                        order.Notes,
                        deliveryAddress);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send admin notification email for order {OrderNumber}", order.OrderNumber);
                }
            });

            return Ok(ApiResponse<string>.SuccessWithData("Order confirmation emails sent successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send order confirmation emails for order {OrderId}", orderId);
            return BadRequest(ApiResponse<string>.Failure($"Failed to send confirmation emails: {ex.Message}"));
        }
    }
}