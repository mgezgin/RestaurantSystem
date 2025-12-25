using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Features.Auth.Commands.LoginCommand;

public class GoogleLoginCommandHandler : ICommandHandler<GoogleLoginCommand, ApiResponse<AuthResponse>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public GoogleLoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<ApiResponse<AuthResponse>> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string>() 
                { 
                    _configuration["Authentication:Google:ClientId"]!,          // Web
                    _configuration["Authentication:Google:AndroidClientId"]!,   // Android
                    _configuration["Authentication:Google:IosClientId"]!        // iOS
                }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);

            var user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = payload.Email,
                    Email = payload.Email,
                    FirstName = payload.GivenName,
                    LastName = payload.FamilyName,
                    EmailConfirmed = true, // Google emails are verified
                    Role = RestaurantSystem.Domain.Common.Enums.UserRole.Customer,
                    CreatedBy = "GoogleAuth",
                    RefreshToken = string.Empty, // Will be set later
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return ApiResponse<AuthResponse>.Failure("Registration failed", string.Join(", ", result.Errors.Select(e => e.Description)));
                }

                await _userManager.AddToRoleAsync(user, "Customer");
            }

            var token = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);

            return ApiResponse<AuthResponse>.SuccessWithData(new AuthResponse
            {
                AccessToken = token,
                RefreshToken = refreshToken,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                UserId = user.Id,
                Expiration = _tokenService.GetAccessTokenExpiration()
            });
        }
        catch (InvalidJwtException)
        {
             return ApiResponse<AuthResponse>.Failure("Invalid token", "The provided Google token is invalid.");
        }
        catch (Exception ex)
        {
            return ApiResponse<AuthResponse>.Failure("Login failed", ex.Message);
        }
    }
}
