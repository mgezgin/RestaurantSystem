using MediatR;
using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.User.Commands.ConfirmAccountDeletionCommand;

public record ConfirmAccountDeletionCommand(Guid UserId, string Token) : ICommand<ApiResponse<string>>;

public class ConfirmAccountDeletionCommandHandler : ICommandHandler<ConfirmAccountDeletionCommand, ApiResponse<string>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfirmAccountDeletionCommandHandler> _logger;

    public ConfirmAccountDeletionCommandHandler(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ILogger<ConfirmAccountDeletionCommandHandler> logger)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(ConfirmAccountDeletionCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(command.UserId.ToString());
        if (user == null)
        {
            return ApiResponse<string>.Failure("User not found or already deleted");
        }

        var isValid = await _userManager.VerifyUserTokenAsync(user, "Default", "AccountDeletion", command.Token);
        if (!isValid)
        {
            return ApiResponse<string>.Failure("Invalid or expired deletion token");
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to delete user {UserId}: {Errors}", user.Id, errors);
            return ApiResponse<string>.Failure($"Failed to delete account: {errors}");
        }

        _logger.LogInformation("User {UserId} permanently deleted via confirmation token", user.Id);

        return ApiResponse<string>.SuccessWithData("Account permanently deleted");
    }
}
