using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Basket.Dtos;
using RestaurantSystem.Api.Features.Basket.Dtos.Requests;
using RestaurantSystem.Api.Features.Basket.Interfaces;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;
using System.Text.Json;

namespace RestaurantSystem.Api.Features.Basket.Services;

public class BasketService : IBasketService
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BasketService> _logger;
    private readonly IConfiguration _configuration;

    private const string BASKET_CACHE_KEY_PREFIX = "basket:";
    private const int BASKET_CACHE_EXPIRY_MINUTES = 30;
    private const decimal TAX_RATE = 0.08m; // 8% tax

    public BasketService(
       ApplicationDbContext context,
       IDistributedCache cache,
       ICurrentUserService currentUserService,
       ILogger<BasketService> logger,
       IConfiguration configuration)
    {
        _context = context;
        _cache = cache;
        _currentUserService = currentUserService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<BasketDto?> GetBasketAsync(string sessionId, Guid? userId = null)
    {
        // Try to get from cache first
        var cacheKey = GetCacheKey(sessionId, userId);
        var cachedBasket = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedBasket))
        {
            return JsonSerializer.Deserialize<BasketDto>(cachedBasket);
        }

        // Get from database
        var basket = await GetBasketFromDatabase(sessionId, userId);
        if (basket == null)
            return null;

        var basketDto = MapToBasketDto(basket);

        // Cache the result
        await CacheBasketAsync(cacheKey, basketDto);

        return basketDto;
    }

    public async Task<BasketDto> AddItemToBasketAsync(string sessionId, Guid? userId, AddToBasketDto item)
    {

        if (item.ProductId == Guid.Empty && item.MenuId == Guid.Empty)
        {
            throw new InvalidOperationException("Product or Menu should be provided");
        }

        var basket = await GetOrCreateBasketAsync(sessionId, userId);

        if (item.MenuId.HasValue && item.MenuId.Value != Guid.Empty)
        {
            var menu = await _context.Menus
                .Include(m => m.MenuItems)
                    .ThenInclude(mi => mi.Product)
                .Include(m => m.MenuItems)
                    .ThenInclude(mi => mi.ProductVariation)
                .FirstOrDefaultAsync(m => m.Id == item.MenuId && m.IsActive && !m.IsDeleted);

            if (menu == null)
                throw new InvalidOperationException("Menu not found or unavailable");

            decimal menuPrice = 0;

            foreach (var menuItem in menu.MenuItems)
            {
                if (menuItem.SpecialPrice.HasValue)
                {
                    // Use special price if available
                    menuPrice += menuItem.SpecialPrice.Value;
                }
                else
                {
                    // Use product base price + variation modifier
                    var itemPrice = menuItem.Product.BasePrice;
                    if (menuItem.ProductVariation != null)
                    {
                        itemPrice += menuItem.ProductVariation.PriceModifier;
                    }
                    menuPrice += itemPrice;
                }
            }

            // Check if menu already exists in basket
            var existingMenuItem = await _context.BasketItems
                .FirstOrDefaultAsync(bi =>
                    bi.BasketId == basket.Id &&
                    bi.MenuId == item.MenuId);

            if (existingMenuItem != null)
            {
                // Update quantity
                existingMenuItem.Quantity += item.Quantity;
                existingMenuItem.ItemTotal = existingMenuItem.Quantity * existingMenuItem.UnitPrice;
                existingMenuItem.UpdatedAt = DateTime.UtcNow;
                existingMenuItem.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";
            }
            else
            {
                // Create new basket item for menu
                var basketItem = new BasketItem
                {
                    BasketId = basket.Id,
                    MenuId = item.MenuId,
                    Quantity = item.Quantity,
                    UnitPrice = menuPrice,
                    ItemTotal = menuPrice * item.Quantity,
                    SpecialInstructions = item.SpecialInstructions,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };

                _context.BasketItems.Add(basketItem);
            }
        }
        else if (item.ProductId != Guid.Empty)
        {
            // Validate product exists and is available
            var product = await _context.Products
                .Include(p => p.Variations)
                .FirstOrDefaultAsync(p => p.Id == item.ProductId && p.IsActive && p.IsAvailable);

            if (product == null)
                throw new InvalidOperationException("Product not found or unavailable");

            // Validate variation if specified
            ProductVariation? variation = null;
            if (item.ProductVariationId.HasValue)
            {
                variation = product.Variations.FirstOrDefault(v => v.Id == item.ProductVariationId.Value && v.IsActive);
                if (variation == null)
                    throw new InvalidOperationException("Product variation not found or unavailable");
            }

            // Check if item already exists in basket
            var existingItem = await _context.BasketItems
                .FirstOrDefaultAsync(bi =>
                    bi.BasketId == basket.Id &&
                    bi.ProductId == item.ProductId &&
                    bi.ProductVariationId == item.ProductVariationId);

            if (existingItem != null)
            {
                // Update quantity
                existingItem.Quantity += item.Quantity;
                existingItem.ItemTotal = existingItem.Quantity * existingItem.UnitPrice;
                existingItem.UpdatedAt = DateTime.UtcNow;
                existingItem.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";
            }
            else
            {
                // Calculate unit price
                var unitPrice = product.BasePrice + (variation?.PriceModifier ?? 0);

                // Create new basket item
                var basketItem = new BasketItem
                {
                    BasketId = basket.Id,
                    ProductId = item.ProductId,
                    ProductVariationId = item.ProductVariationId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice,
                    ItemTotal = unitPrice * item.Quantity,
                    SpecialInstructions = item.SpecialInstructions,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
                };

                // Add side items if any

                _context.BasketItems.Add(basketItem);
            }
        }

        await _context.SaveChangesAsync();
        await RecalculateBasketTotalsAsync(basket.Id);

        // Invalidate cache
        await InvalidateBasketCacheAsync(sessionId, userId);

        return await GetBasketAsync(sessionId, userId) ?? throw new InvalidOperationException("Failed to retrieve basket");
    }

    public async Task<BasketDto> UpdateBasketItemAsync(string sessionId, Guid basketItemId, UpdateBasketItemDto update)
    {
        var basketItem = await _context.BasketItems
            .Include(bi => bi.Basket)
            .FirstOrDefaultAsync(bi => bi.Id == basketItemId && bi.Basket.SessionId == sessionId);

        if (basketItem == null)
            throw new InvalidOperationException("Basket item not found");

        basketItem.Quantity = update.Quantity;
        basketItem.ItemTotal = basketItem.Quantity * basketItem.UnitPrice;
        basketItem.SpecialInstructions = update.SpecialInstructions;
        basketItem.UpdatedAt = DateTime.UtcNow;
        basketItem.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync();
        await RecalculateBasketTotalsAsync(basketItem.BasketId);

        // Invalidate cache
        await InvalidateBasketCacheAsync(sessionId, basketItem.Basket.UserId);

        return await GetBasketAsync(sessionId, basketItem.Basket.UserId) ?? throw new InvalidOperationException("Failed to retrieve basket");
    }

    public async Task<BasketDto> RemoveItemFromBasketAsync(string sessionId, Guid basketItemId)
    {
        var basketItem = await _context.BasketItems
            .Include(bi => bi.Basket)
            .FirstOrDefaultAsync(bi => bi.Id == basketItemId && bi.Basket.SessionId == sessionId);

        if (basketItem == null)
            throw new InvalidOperationException("Basket item not found");

        var basketId = basketItem.BasketId;
        var userId = basketItem.Basket.UserId;

        _context.BasketItems.Remove(basketItem);

        await _context.SaveChangesAsync();
        await RecalculateBasketTotalsAsync(basketId);

        // Invalidate cache
        await InvalidateBasketCacheAsync(sessionId, userId);

        return await GetBasketAsync(sessionId, userId) ?? throw new InvalidOperationException("Failed to retrieve basket");
    }


    public async Task<BasketDto> ClearBasketAsync(string sessionId)
    {
        var basket = await GetBasketFromDatabase(sessionId, _currentUserService.UserId);
        if (basket == null)
            throw new InvalidOperationException("Basket not found");

        // Remove all items
        var items = await _context.BasketItems
            .Where(bi => bi.BasketId == basket.Id)
            .ToListAsync();

        foreach (var item in items)
        {
            _context.BasketItems.Remove(item);
        }

        basket.SubTotal = 0;
        basket.Tax = 0;
        basket.Total = 0;
        basket.UpdatedAt = DateTime.UtcNow;
        basket.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync();

        // Invalidate cache
        await InvalidateBasketCacheAsync(sessionId, basket.UserId);

        return MapToBasketDto(basket);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<BasketDto> ApplyPromoCodeAsync(string sessionId, string promoCode)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        // TODO: Implement promo code logic
        throw new NotImplementedException("Promo code functionality not yet implemented");
    }

    public async Task<BasketDto> RemovePromoCodeAsync(string sessionId)
    {
        var basket = await GetBasketFromDatabase(sessionId, _currentUserService.UserId);
        if (basket == null)
            throw new InvalidOperationException("Basket not found");

        basket.PromoCode = null;
        basket.Discount = 0;
        basket.UpdatedAt = DateTime.UtcNow;
        basket.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync();
        await RecalculateBasketTotalsAsync(basket.Id);

        // Invalidate cache
        await InvalidateBasketCacheAsync(sessionId, basket.UserId);

        return await GetBasketAsync(sessionId, basket.UserId) ?? throw new InvalidOperationException("Failed to retrieve basket");
    }

    public async Task<BasketSummaryDto?> GetBasketSummaryAsync(string sessionId, Guid? userId = null)
    {
        var basket = await GetBasketAsync(sessionId, userId);
        if (basket == null)
            return null;

        return new BasketSummaryDto
        {
            Id = basket.Id,
            ItemCount = basket.Items.Sum(i => i.Quantity),
            Total = basket.Total
        };
    }

    public async Task<BasketDto> MergeAnonymousBasketAsync(string sessionId, Guid userId)
    {
        var anonymousBasket = await GetBasketFromDatabase(sessionId, null);
        var userBasket = await GetBasketFromDatabase(null, userId);

        if (anonymousBasket == null)
        {
            return userBasket != null
                ? MapToBasketDto(userBasket)
                : MapToBasketDto(await GetOrCreateBasketAsync(sessionId, userId));
        }

        if (userBasket == null)
        {
            // Assign anonymous basket to user
            anonymousBasket.UserId = userId;
            anonymousBasket.UpdatedAt = DateTime.UtcNow;
            anonymousBasket.UpdatedBy = userId.ToString();
            await _context.SaveChangesAsync();

            // Invalidate cache
            await InvalidateBasketCacheAsync(sessionId, null);
            await InvalidateBasketCacheAsync(sessionId, userId);

            return MapToBasketDto(anonymousBasket);
        }

        // Merge anonymous items into user basket
        var anonymousItems = await _context.BasketItems
            .Where(bi => bi.BasketId == anonymousBasket.Id)
            .ToListAsync();

        foreach (var item in anonymousItems)
        {
            var existingItem = await _context.BasketItems
                .FirstOrDefaultAsync(bi =>
                    bi.BasketId == userBasket.Id &&
                    bi.ProductId == item.ProductId &&
                    bi.ProductVariationId == item.ProductVariationId);

            if (existingItem != null)
            {
                existingItem.Quantity += item.Quantity;
                existingItem.ItemTotal = existingItem.Quantity * existingItem.UnitPrice;
                existingItem.UpdatedAt = DateTime.UtcNow;
                existingItem.UpdatedBy = userId.ToString();
            }
            else
            {
                item.BasketId = userBasket.Id;
                item.UpdatedAt = DateTime.UtcNow;
                item.UpdatedBy = userId.ToString();
            }
        }

        // Delete anonymous basket
        anonymousBasket.IsDeleted = true;
        anonymousBasket.DeletedAt = DateTime.UtcNow;
        anonymousBasket.DeletedBy = userId.ToString();

        await _context.SaveChangesAsync();
        await RecalculateBasketTotalsAsync(userBasket.Id);

        // Invalidate cache
        await InvalidateBasketCacheAsync(sessionId, null);
        await InvalidateBasketCacheAsync(sessionId, userId);

        return await GetBasketAsync(sessionId, userId) ?? throw new InvalidOperationException("Failed to retrieve basket");
    }

    public async Task RecalculateBasketTotalsAsync(Guid basketId)
    {
        var basket = await _context.Baskets
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == basketId);

        if (basket == null)
            return;

        decimal subTotal = 0;

        foreach (var item in basket.Items)
        {
            var itemTotal = item.ItemTotal;
            subTotal += itemTotal;
        }

        basket.SubTotal = subTotal;
        basket.Tax = Math.Round(subTotal * TAX_RATE, 2);
        basket.Total = basket.SubTotal + basket.Tax + basket.DeliveryFee - basket.Discount;
        basket.UpdatedAt = DateTime.UtcNow;
        basket.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync();
    }

    private async Task<Domain.Entities.Basket> GetOrCreateBasketAsync(string? sessionId, Guid? userId)
    {
        var basket = await GetBasketFromDatabase(sessionId, userId);

        if (basket == null)
        {
            basket = new Domain.Entities.Basket
            {
                UserId = userId,
                SessionId = sessionId ?? Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId?.ToString() ?? "System"
            };

            _context.Baskets.Add(basket);
            await _context.SaveChangesAsync();
        }

        return basket;
    }

    private async Task<Domain.Entities.Basket?> GetBasketFromDatabase(string? sessionId, Guid? userId)
    {
        var query = _context.Baskets
            .Include(b => b.Items)
                .ThenInclude(bi => bi.Product)
            .Include(b => b.Items)
                .ThenInclude(bi => bi.ProductVariation)
            .Include(b => b.Items)
                .ThenInclude(bi => bi.Menu)
                .ThenInclude(b => b.MenuItems)
            .Include(b => b.Items)
            .Where(b => !b.IsDeleted);

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            query = query.Where(b => b.UserId == userId.Value);
        }
        else if (!string.IsNullOrEmpty(sessionId))
        {
            query = query.Where(b => b.SessionId == sessionId && b.UserId == null || b.UserId == Guid.Empty);
        }
        else
        {
            return null;
        }

        return await query.FirstOrDefaultAsync();
    }

    private BasketDto MapToBasketDto(Domain.Entities.Basket basket)
    {
        return new BasketDto
        {
            Id = basket.Id,
            UserId = basket.UserId != Guid.Empty ? basket.UserId : null,
            SessionId = basket.SessionId,
            SubTotal = basket.SubTotal,
            Tax = basket.Tax,
            DeliveryFee = basket.DeliveryFee,
            Discount = basket.Discount,
            Total = basket.Total,
            PromoCode = basket.PromoCode,
            TotalItems = basket.Items.Sum(i => i.Quantity),
            ExpiresAt = basket.ExpiresAt,
            Notes = basket.Notes,
            Items = basket.Items.Select(item => new BasketItemDto
            {
                Id = item.Id,
                ProductId = item.ProductId,
                ProductName = item.Product != null ? item.Product.Name : item.Menu?.Name ?? string.Empty,
                MenuId = item.MenuId,
                ProductDescription = item.Product != null ? item.Product.Description : item.Menu?.Description ?? string.Empty,
                ProductImageUrl = item.Product?.ImageUrl ?? string.Empty,
                ProductVariationId = item.ProductVariationId,
                VariationName = item.ProductVariation?.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                ItemTotal = item.ItemTotal,
                SpecialInstructions = item.SpecialInstructions
            }).ToList()
        };
    }

    private string GetCacheKey(string? sessionId, Guid? userId)
    {
        if (userId.HasValue && userId.Value != Guid.Empty)
            return $"{BASKET_CACHE_KEY_PREFIX}user:{userId}";

        return $"{BASKET_CACHE_KEY_PREFIX}session:{sessionId}";
    }

    private async Task CacheBasketAsync(string cacheKey, BasketDto basket)
    {
        var options = new DistributedCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(BASKET_CACHE_EXPIRY_MINUTES)
        };

        var json = JsonSerializer.Serialize(basket);
        await _cache.SetStringAsync(cacheKey, json, options);
    }

    private async Task InvalidateBasketCacheAsync(string? sessionId, Guid? userId)
    {
        var cacheKey = GetCacheKey(sessionId, userId);
        await _cache.RemoveAsync(cacheKey);
    }
}
