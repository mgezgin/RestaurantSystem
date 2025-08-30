using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Orders.Commands.AddPaymentToOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.CancelOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.CreateOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.RefundPaymentCommand;
using RestaurantSystem.Api.Features.Orders.Commands.ToggleFocusOrderCommand;
using RestaurantSystem.Api.Features.Orders.Commands.UpdateOrderStatusCommand;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Api.Features.Orders.Queries.GetOrderByIdQuery;
using RestaurantSystem.Api.Features.Orders.Queries.GetOrdersQuery;
using RestaurantSystem.Api.Features.Orders.Services;
using RestaurantSystem.Api.Features.Products.Queries.GetFocusOrdersQuery;

namespace RestaurantSystem.Api.Features.Orders;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly CustomMediator _mediator;
    private readonly IOrderEventService _orderEventService;

    public OrdersController(CustomMediator mediator,IOrderEventService orderEventService)
    {
        _mediator = mediator;
        _orderEventService = orderEventService;

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
    [Authorize]
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
    public async Task<ActionResult<ApiResponse<OrderPaymentDto>>> AddPayment(
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
}