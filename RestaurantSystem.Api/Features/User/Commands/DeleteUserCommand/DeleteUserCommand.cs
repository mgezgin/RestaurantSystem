using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Products.Commands.DeleteProductImageCommand;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.User.Commands.DeleteUserCommand;

public record DeleteUserCommand(Guid UserId) : ICommand<ApiResponse<string>>;

public class DeleteUserCommandHandler : ICommandHandler<DeleteUserCommand, ApiResponse<string>>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteProductImageCommandHandler> _logger;

    public DeleteUserCommandHandler(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        ICurrentUserService currentUserService,
        ILogger<DeleteProductImageCommandHandler> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(i => i.Id == command.UserId && !i.IsDeleted, cancellationToken);

        if (user == null)
        {
            return ApiResponse<string>.Failure("User not found");
        }

        // Hard delete staff members (Server, Cashier, KitchenStaff, Admin)
        // Soft delete customers for data retention
        if (user.Role == UserRole.Server || user.Role == UserRole.Cashier || 
            user.Role == UserRole.KitchenStaff || user.Role == UserRole.Admin)
        {
            // Hard delete - permanently remove from database
            _context.Users.Remove(user);
            
            _logger.LogInformation("Staff user {UserId} ({Role}) permanently deleted by user {DeletedBy}",
                command.UserId, user.Role, _currentUserService.UserId);
        }
        else
        {
            // Soft delete for customers
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            user.DeletedBy = _currentUserService.UserId?.ToString() ?? "System";
            
            _logger.LogInformation("Customer {UserId} soft deleted by user {DeletedBy}",
                command.UserId, _currentUserService.UserId);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.SuccessWithData("User deleted successfully");
    }
}