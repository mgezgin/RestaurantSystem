using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Auth.Commands.LoginCommand;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Api.Features.Auth.Interfaces;
using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly CustomMediator _mediator;

    public AuthController(IAuthService authService, CustomMediator mediator)
    {
        _authService = authService;
        _mediator = mediator;
    }

    [HttpPost("register/customer")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(ApiResponse<AuthResponse>.SuccessWithData(result, "User registered successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.Failure(ex.Message));
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginCommand request)
    {
        try
        {
            return Ok(await _mediator.SendCommand(request));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.Failure(ex.Message));
        }
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request);
            return Ok(ApiResponse<AuthResponse>.SuccessWithData(result, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<AuthResponse>.Failure(ex.Message));
        }
    }

    [HttpGet("test-auth")]
    [Authorize]
    public ActionResult<ApiResponse<string>> TestAuth()
    {
        return Ok(ApiResponse<string>.SuccessWithData("You are authenticated!"));
    }

    [HttpGet("admin-only")]
    [RequireAdmin]
    public ActionResult<ApiResponse<string>> AdminOnly()
    {
        return Ok(ApiResponse<string>.SuccessWithData("You are an admin!"));
    }
}
