using Microsoft.AspNetCore.Identity;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Domain.Common;
using System.Security.Claims;
using RestaurantSystem.Api.Features.Auth.Interfaces;
using RestaurantSystem.Api.Common.Services.Interfaces;

namespace RestaurantSystem.Api.Features.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new Exception("User with this email already exists");
        }

        // Create new user
        var newUser = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "System",
            RefreshToken = _tokenService.GenerateRefreshToken()
        };

        var result = await _userManager.CreateAsync(newUser, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new Exception($"Failed to create user: {errors}");
        }

        // Add role to user
        await _userManager.AddToRoleAsync(newUser, request.Role.ToString());

        // Generate tokens
        var token = _tokenService.GenerateAccessToken(newUser);
        newUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(newUser);

        // Return response
        return new AuthResponse
        {
            UserId = newUser.Id,
            FirstName = newUser.FirstName,
            LastName = newUser.LastName,
            Email = newUser.Email!,
            Role = newUser.Role,
            AccessToken = token,
            RefreshToken = newUser.RefreshToken,
            Expiration = _tokenService.GetAccessTokenExpiration()
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null || user.IsDeleted)
        {
            throw new Exception("Invalid credentials");
        }

        // Verify password
        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            throw new Exception("Invalid credentials");
        }

        // Generate tokens
        var token = _tokenService.GenerateAccessToken(user);
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        // Return response
        return new AuthResponse
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
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            throw new Exception("Invalid token");
        }

        var user = await _userManager.FindByIdAsync(userId);

        if (user == null || user.IsDeleted ||
            user.RefreshToken != request.RefreshToken ||
            user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            throw new Exception("Invalid token");
        }

        // Generate new tokens
        var newToken = _tokenService.GenerateAccessToken(user);
        user.RefreshToken = _tokenService.GenerateRefreshToken();
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
        await _userManager.UpdateAsync(user);

        // Return response
        return new AuthResponse
        {
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email!,
            Role = user.Role,
            AccessToken = newToken,
            RefreshToken = user.RefreshToken,
            Expiration = _tokenService.GetAccessTokenExpiration()
        };
    }
}
