using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Auth.Dtos;
using RestaurantSystem.Api.Features.User.Commands.DeleteUserCommand;
using RestaurantSystem.Api.Features.User.Commands.EditStaffCommand;
using RestaurantSystem.Api.Features.User.Commands.RegisterCustomerCommand;
using RestaurantSystem.Api.Features.User.Commands.RegisterStaffCommand;
using RestaurantSystem.Api.Features.User.Commands.UpdateUserDiscountsCommand;
using RestaurantSystem.Api.Features.User.Commands.UpdateUserProfileCommand;
using RestaurantSystem.Api.Features.User.Dtos;
using RestaurantSystem.Api.Features.User.Queries.GetUsersQuery;

namespace RestaurantSystem.Api.Features.User;
[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly CustomMediator _mediator;

    public UserController(CustomMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all users with optional filters
    /// </summary>
    [HttpGet("users")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] GetUsersQuery query)
    {
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Register a new customer (public registration)
    /// </summary>
    [HttpPost("register/customer")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterCustomer([FromBody] RegisterCustomerCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Register a new user with specific role (admin only)
    /// </summary>
    [HttpPost("register/staff")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RegisterStaff([FromBody] RegisterStaffCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Register a new user with specific role (admin only)
    /// </summary>
    [HttpPost("update/staff")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> UpdateStaff([FromBody] UpdateStaffCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateProfile([FromBody] UpdateUserProfileCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Update user settings (admin only)
    /// </summary>
    [HttpPut("user-discounts")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateUserDiscountSettings([FromBody] UpdateUserDiscountsCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Register a new user with specific role (admin only)
    /// </summary>
    [HttpDelete("delete/user")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<string>>> DeleteUser([FromBody] DeleteUserCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }
}
