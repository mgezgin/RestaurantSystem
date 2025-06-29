using RestaurantSystem.Api.Features.Basket.Dtos.Requests;
using RestaurantSystem.Api.Features.Basket.Dtos;

namespace RestaurantSystem.Api.Features.Basket.Interfaces;

public interface IBasketService
{
    Task<BasketDto?> GetBasketAsync(string sessionId, Guid? userId = null);
    Task<BasketDto> AddItemToBasketAsync(string sessionId, Guid? userId, AddToBasketDto item);
    Task<BasketDto> UpdateBasketItemAsync(string sessionId, Guid basketItemId, UpdateBasketItemDto update);
    Task<BasketDto> RemoveItemFromBasketAsync(string sessionId, Guid basketItemId);
    Task<BasketDto> ClearBasketAsync(string sessionId);
    Task<BasketDto> ApplyPromoCodeAsync(string sessionId, string promoCode);
    Task<BasketDto> RemovePromoCodeAsync(string sessionId);
    Task<BasketSummaryDto?> GetBasketSummaryAsync(string sessionId, Guid? userId = null);
    Task<BasketDto> MergeAnonymousBasketAsync(string sessionId, Guid userId);
    Task RecalculateBasketTotalsAsync(Guid basketId);

}
