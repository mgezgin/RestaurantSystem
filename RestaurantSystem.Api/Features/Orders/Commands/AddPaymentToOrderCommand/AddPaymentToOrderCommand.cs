using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Orders.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Orders.Commands.AddPaymentToOrderCommand;

public record AddPaymentToOrderCommand : ICommand<ApiResponse<OrderPaymentDto>>
{
    public Guid OrderId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal Amount { get; set; }
    public string? TransactionId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? CardLastFourDigits { get; set; }
    public string? CardType { get; set; }
    public string? PaymentGateway { get; set; }
    public string? PaymentNotes { get; set; }
}

public class AddPaymentToOrderCommandHandler : ICommandHandler<AddPaymentToOrderCommand, ApiResponse<OrderPaymentDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddPaymentToOrderCommandHandler> _logger;

    public AddPaymentToOrderCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<AddPaymentToOrderCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderPaymentDto>> Handle(AddPaymentToOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId && !o.IsDeleted, cancellationToken);

        if (order == null)
        {
            return ApiResponse<OrderPaymentDto>.Failure("Order not found");
        }

        if (order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Completed)
        {
            return ApiResponse<OrderPaymentDto>.Failure($"Cannot add payment to {order.Status} order");
        }

        // Check if order is already fully paid
        if (order.RemainingAmount <= 0 && command.Amount > 0)
        {
            return ApiResponse<OrderPaymentDto>.Failure("Order is already fully paid");
        }

        // Remove any existing Pending placeholder payments from order creation
        // These are placeholder payments that should be replaced when the actual payment is added
        var pendingPlaceholders = order.Payments.Where(p => p.Status == PaymentStatus.Pending).ToList();
        foreach (var placeholder in pendingPlaceholders)
        {
            _context.OrderPayments.Remove(placeholder);
            order.Payments.Remove(placeholder);
        }

        // Save the removal of placeholder payments first
        await _context.SaveChangesAsync(cancellationToken);

        var payment = new OrderPayment
        {
            OrderId = order.Id,
            PaymentMethod = command.PaymentMethod,
            Amount = Math.Min(command.Amount, order.RemainingAmount), // Don't allow overpayment
            Status = PaymentStatus.Pending,
            TransactionId = command.TransactionId,
            ReferenceNumber = command.ReferenceNumber,
            CardLastFourDigits = command.CardLastFourDigits,
            CardType = command.CardType,
            PaymentGateway = command.PaymentGateway,
            PaymentNotes = command.PaymentNotes,
            PaymentDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
        };

        _context.OrderPayments.Add(payment);
        order.Payments.Add(payment);

        // Process payment (integrate with payment gateway for non-cash payments)
        if (payment.PaymentMethod != PaymentMethod.Cash)
        {
            // TODO: Integrate with payment gateway
            // For now, mark as completed
            payment.Status = PaymentStatus.Completed;
        }
        else
        {
            payment.Status = PaymentStatus.Completed;
        }

        // Update order payment summary - sum all completed payments from database
        // Reload payments from database to ensure we have the current state after removing placeholder
        var currentPayments = await _context.OrderPayments
            .Where(p => p.OrderId == order.Id && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var completedPaymentTotal = currentPayments.Where(p => p.Status == PaymentStatus.Completed).Sum(p => p.Amount);
        order.TotalPaid = completedPaymentTotal;
        order.RemainingAmount = order.Total - order.TotalPaid;

        // Update order payment status with tolerance for floating point precision in financial calculations
        // Use 0.01 (1 cent) as tolerance to handle rounding errors
        const decimal tolerance = 0.01m;
        if (order.RemainingAmount <= tolerance)
        {
            // If remaining is within tolerance of zero or negative, it's fully paid or overpaid
            order.PaymentStatus = order.RemainingAmount < -tolerance ? PaymentStatus.Overpaid : PaymentStatus.Completed;
        }
        else if (order.TotalPaid > 0)
        {
            order.PaymentStatus = PaymentStatus.PartiallyPaid;
        }

        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        var paymentDto = new OrderPaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            PaymentMethod = payment.PaymentMethod.ToString(),
            Amount = payment.Amount,
            Status = payment.Status.ToString(),
            TransactionId = payment.TransactionId,
            ReferenceNumber = payment.ReferenceNumber,
            PaymentDate = payment.PaymentDate,
            CardLastFourDigits = payment.CardLastFourDigits,
            CardType = payment.CardType,
            PaymentGateway = payment.PaymentGateway,
            PaymentNotes = payment.PaymentNotes,
            IsRefunded = payment.IsRefunded,
            RefundedAmount = payment.RefundedAmount,
            RefundDate = payment.RefundDate,
            RefundReason = payment.RefundReason
        };

        _logger.LogInformation("Payment {PaymentId} added to order {OrderNumber} by user {UserId}",
            payment.Id, order.OrderNumber, _currentUserService.UserId);

        return ApiResponse<OrderPaymentDto>.SuccessWithData(paymentDto, "Payment added successfully");
    }
}
