using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Basket.Interfaces;

namespace RestaurantSystem.Api.Common.Services;

public class BasketMergeService : IBasketMergeService
{
    private readonly IBasketService _basketService;
    private readonly ILogger<BasketMergeService> _logger;

    public BasketMergeService(IBasketService basketService, ILogger<BasketMergeService> logger)
    {
        _basketService = basketService;
        _logger = logger;
    }

    public async Task MergeBasketOnLoginAsync(Guid userId, string sessionId)
    {
        try
        {
            await _basketService.MergeAnonymousBasketAsync(sessionId, userId);
            _logger.LogInformation("Successfully merged anonymous basket for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to merge basket for user {UserId}", userId);
            // Don't throw - basket merge should not break login flow
        }
    }
}
