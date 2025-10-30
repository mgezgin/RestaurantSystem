using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Reservations.Commands.CancelReservationCommand;

public record CancelReservationCommand(Guid ReservationId) : ICommand<ApiResponse<bool>>;

public class CancelReservationCommandHandler : ICommandHandler<CancelReservationCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CancelReservationCommandHandler> _logger;

    public CancelReservationCommandHandler(ApplicationDbContext context, ILogger<CancelReservationCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(CancelReservationCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r => r.Id == command.ReservationId, cancellationToken);

            if (reservation == null)
            {
                return ApiResponse<bool>.Failure("Reservation not found");
            }

            if (reservation.Status == ReservationStatus.Cancelled)
            {
                return ApiResponse<bool>.Failure("Reservation is already cancelled");
            }

            if (reservation.Status == ReservationStatus.Completed)
            {
                return ApiResponse<bool>.Failure("Cannot cancel a completed reservation");
            }

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Cancelled reservation {ReservationId}", reservation.Id);
            return ApiResponse<bool>.SuccessWithData(true, "Reservation cancelled successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling reservation {ReservationId}", command.ReservationId);
            return ApiResponse<bool>.Failure("Failed to cancel reservation");
        }
    }
}
