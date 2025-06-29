namespace RestaurantSystem.Api.Common.Services.Interfaces;

public interface IBasketMergeService
{
    Task MergeBasketOnLoginAsync(Guid userId, string sessionId);
}
