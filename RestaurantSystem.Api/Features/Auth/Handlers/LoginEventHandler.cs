using RestaurantSystem.Api.Common.Services.Interfaces;

namespace RestaurantSystem.Api.Features.Auth.Handlers;

public class LoginEventHandler
{
    private readonly IBasketMergeService _basketMergeService;
    private readonly ILogger<LoginEventHandler> _logger;

    public LoginEventHandler(IBasketMergeService basketMergeService, ILogger<LoginEventHandler> logger)
    {
        _basketMergeService = basketMergeService;
        _logger = logger;
    }

    public async Task HandleUserLogin(Guid userId, string sessionId)
    {
        try
        {
            await _basketMergeService.MergeBasketOnLoginAsync(userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling login event for user {UserId}", userId);
        }
    }
}
