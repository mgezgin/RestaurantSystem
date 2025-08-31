using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Features.Auth.Commands.LoginCommand;

public record LoginCommand(string Email, string Password) : ICommand<ApiResponse<AuthResponse>>;

public class LoginCommandHandler : ICommandHandler<LoginCommand, ApiResponse<AuthResponse>>
{

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(UserManager<ApplicationUser> userManager, IConfiguration configuration,ITokenService tokenService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
    {

        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user == null || user.IsDeleted)
        {
            throw new Exception("Invalid credentials");
        }

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, command.Password);

        if (!isPasswordValid)
        {
            throw new Exception("Invalid credentials");
        }

        // Generate tokens
        var token = _tokenService.GenerateAccessToken(user);
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        var authResponse = new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            Role = user.Role,
            AccessToken = token,
            RefreshToken = user.RefreshToken,
            Expiration = _tokenService.GetAccessTokenExpiration()
        };

        return ApiResponse<AuthResponse>.SuccessWithData(authResponse, "User logged in successfully");

    }
}
