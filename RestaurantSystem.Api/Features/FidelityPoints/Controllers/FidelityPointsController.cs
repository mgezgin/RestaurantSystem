using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.FidelityPoints.Dtos;
using RestaurantSystem.Api.Features.FidelityPoints.Interfaces;

namespace RestaurantSystem.Api.Features.FidelityPoints.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FidelityPointsController : ControllerBase
{
    private readonly IFidelityPointsService _fidelityPointsService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<FidelityPointsController> _logger;

    public FidelityPointsController(
        IFidelityPointsService fidelityPointsService,
        ICurrentUserService currentUserService,
        ILogger<FidelityPointsController> logger)
    {
        _fidelityPointsService = fidelityPointsService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's fidelity points balance
    /// </summary>
    [HttpGet("balance")]
    [ProducesResponseType(typeof(ApiResponse<FidelityPointBalanceDto>), 200)]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated"));

        var balance = await _fidelityPointsService.GetUserBalanceAsync(userId.Value, cancellationToken);

        if (balance == null)
        {
            // Return zero balance if user doesn't have one yet
            var emptyBalance = new FidelityPointBalanceDto
            {
                Id = Guid.Empty,
                UserId = userId.Value,
                CurrentPoints = 0,
                TotalEarnedPoints = 0,
                TotalRedeemedPoints = 0,
                LastUpdated = DateTime.UtcNow
            };
            return Ok(ApiResponse<FidelityPointBalanceDto>.SuccessWithData(emptyBalance));
        }

        var dto = new FidelityPointBalanceDto
        {
            Id = balance.Id,
            UserId = balance.UserId,
            CurrentPoints = balance.CurrentPoints,
            TotalEarnedPoints = balance.TotalEarnedPoints,
            TotalRedeemedPoints = balance.TotalRedeemedPoints,
            LastUpdated = balance.LastUpdated
        };

        return Ok(ApiResponse<FidelityPointBalanceDto>.SuccessWithData(dto));
    }

    /// <summary>
    /// Get current user's fidelity points transaction history
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<List<FidelityPointsTransactionDto>>), 200)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.Failure("User not authenticated"));

        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var transactions = await _fidelityPointsService.GetPointsHistoryAsync(
            userId.Value, 
            page, 
            pageSize, 
            cancellationToken);

        var dtos = transactions.Select(t => new FidelityPointsTransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            OrderId = t.OrderId,
            TransactionType = t.TransactionType.ToString(),
            Points = t.Points,
            OrderTotal = t.OrderTotal,
            Description = t.Description,
            ExpiresAt = t.ExpiresAt,
            CreatedAt = t.CreatedAt
        }).ToList();

        return Ok(ApiResponse<List<FidelityPointsTransactionDto>>.SuccessWithData(dtos));
    }

    /// <summary>
    /// Calculate discount amount for a given number of points
    /// </summary>
    [HttpGet("calculate-discount")]
    [ProducesResponseType(typeof(ApiResponse<decimal>), 200)]
    public IActionResult CalculateDiscount([FromQuery] int points)
    {
        if (points <= 0)
            return BadRequest(ApiResponse<object>.Failure("Points must be positive"));

        var discountAmount = _fidelityPointsService.CalculateDiscountFromPoints(points);
        return Ok(ApiResponse<decimal>.SuccessWithData(discountAmount));
    }

    /// <summary>
    /// Calculate points needed for a given discount amount
    /// </summary>
    [HttpGet("calculate-points")]
    [ProducesResponseType(typeof(ApiResponse<int>), 200)]
    public IActionResult CalculatePoints([FromQuery] decimal discountAmount)
    {
        if (discountAmount <= 0)
            return BadRequest(ApiResponse<object>.Failure("Discount amount must be positive"));

        var points = _fidelityPointsService.CalculatePointsForDiscount(discountAmount);
        return Ok(ApiResponse<int>.SuccessWithData(points));
    }
}
