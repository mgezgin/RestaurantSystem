using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Domain.Common;

namespace RestaurantSystem.Api.Features.Auth.Commands.VerifyEmailCommand;

public record VerifyEmailCommand(string Email, string Token) : ICommand<ApiResponse<string>>;

public class VerifyEmailCommandHandler : ICommandHandler<VerifyEmailCommand, ApiResponse<string>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user == null || user.IsDeleted)
        {
            _logger.LogWarning("Email verification attempted for non-existent email: {Email}", command.Email);
            return ApiResponse<string>.Failure("Invalid verification request", "Email verification failed");
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse<string>.SuccessWithData(
                "Email is already verified.",
                "Email verification completed");
        }

        var result = await _userManager.ConfirmEmailAsync(user, command.Token);

        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            _logger.LogWarning("Email verification failed for user {UserId}: {Errors}", user.Id, string.Join(", ", errors));
            return ApiResponse<string>.Failure(errors, "Email verification failed");
        }

        // Update audit fields
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = "EmailVerification";
        await _userManager.UpdateAsync(user);

        _logger.LogInformation("Email successfully verified for user {UserId}", user.Id);

        return ApiResponse<string>.SuccessWithData(
            "Email has been verified successfully",
            "Email verification completed");
    }
}
