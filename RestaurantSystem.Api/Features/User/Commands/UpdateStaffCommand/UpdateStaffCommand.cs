using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Domain.Common.Enums;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Features.User.Commands.EditStaffCommand;

public record UpdateStaffCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    UserRole Role) : ICommand<ApiResponse<AuthResponse>>;

public class UpdateStaffCommandHandler : ICommandHandler<UpdateStaffCommand, ApiResponse<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly ILogger<UpdateStaffCommandHandler> _logger;

    public UpdateStaffCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        ILogger<UpdateStaffCommandHandler> logger)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(UpdateStaffCommand command, CancellationToken cancellationToken)
    {
        // Check if current user is admin (this endpoint should be admin-only)
        var currentUser = await _currentUserService.GetUserAsync();

        if (currentUser == null || currentUser.Role != UserRole.Admin)
        {
            return ApiResponse<AuthResponse>.Failure("Unauthorized access", "Only administrators can register users with roles");
        }

        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(command.Email);

        if (existingUser == null)
        {
            return ApiResponse<AuthResponse>.Failure("User doesn't exist", "Update failed");
        }

        // Create new user

        string resetToken = await _userManager.GeneratePasswordResetTokenAsync(existingUser);

        await _userManager.ResetPasswordAsync(existingUser, resetToken, command.Password);
        
        var emailtoken = await _userManager.GenerateChangeEmailTokenAsync(existingUser,command.Email);
        await _userManager.ChangeEmailAsync(existingUser,command.Email, emailtoken);


        // Generate tokens
        var token = _tokenService.GenerateAccessToken(existingUser);
        existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

        existingUser.UserName = command.FirstName;
        existingUser.LastName = command.LastName;
        existingUser.Role = command.Role;

        await _userManager.UpdateAsync(existingUser);

        // Return response
        var authResponse = new AuthResponse
        {
            UserId = existingUser.Id,
            FirstName = existingUser.FirstName,
            LastName = existingUser.LastName,
            Email = existingUser.Email!,
            Role = existingUser.Role,
            AccessToken = token,
            RefreshToken = existingUser.RefreshToken,
            Expiration = _tokenService.GetAccessTokenExpiration()
        };

        return ApiResponse<AuthResponse>.SuccessWithData(authResponse, $"User registered successfully with role {command.Role}");
    }
}