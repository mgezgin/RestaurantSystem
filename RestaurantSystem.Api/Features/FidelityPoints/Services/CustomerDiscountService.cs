using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.FidelityPoints.Interfaces;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.FidelityPoints.Services;

public class CustomerDiscountService : ICustomerDiscountService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CustomerDiscountService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<List<CustomerDiscountRule>> GetActiveDiscountsForUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        
        return await _context.CustomerDiscountRules
            .AsNoTracking()
            .Where(d => d.UserId == userId 
                && d.IsActive
                && (d.ValidFrom == null || d.ValidFrom <= now)
                && (d.ValidUntil == null || d.ValidUntil >= now)
                && (d.MaxUsageCount == null || d.UsageCount < d.MaxUsageCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDiscountRule?> FindBestApplicableDiscountAsync(
        Guid userId, 
        decimal orderAmount, 
        CancellationToken cancellationToken = default)
    {
        var activeDiscounts = await GetActiveDiscountsForUserAsync(userId, cancellationToken);

        // Filter by order amount constraints
        var applicableDiscounts = activeDiscounts
            .Where(d => IsDiscountValid(d, orderAmount))
            .ToList();

        if (!applicableDiscounts.Any())
            return null;

        // Find the discount that gives the maximum discount amount
        return applicableDiscounts
            .OrderByDescending(d => CalculateDiscountAmount(d, orderAmount))
            .FirstOrDefault();
    }

    public decimal CalculateDiscountAmount(CustomerDiscountRule rule, decimal orderAmount)
    {
        if (rule.DiscountType == DiscountType.Percentage)
        {
            return orderAmount * (rule.DiscountValue / 100);
        }
        else // FixedAmount
        {
            return rule.DiscountValue;
        }
    }

    public async Task<CustomerDiscountRule> ApplyDiscountAsync(
        Guid discountRuleId, 
        CancellationToken cancellationToken = default)
    {
        var discount = await _context.CustomerDiscountRules
            .FirstOrDefaultAsync(d => d.Id == discountRuleId, cancellationToken);

        if (discount == null)
            throw new InvalidOperationException($"Discount rule with ID {discountRuleId} not found");

        // Increment usage count
        discount.UsageCount++;
        discount.UpdatedAt = DateTime.UtcNow;
        discount.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        // Deactivate if max usage reached
        if (discount.MaxUsageCount.HasValue && discount.UsageCount >= discount.MaxUsageCount.Value)
        {
            discount.IsActive = false;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return discount;
    }

    public async Task<CustomerDiscountRule?> GetDiscountByIdAsync(
        Guid discountId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.CustomerDiscountRules
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == discountId, cancellationToken);
    }

    public async Task<List<CustomerDiscountRule>> GetAllDiscountsForUserAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        return await _context.CustomerDiscountRules
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerDiscountRule> CreateDiscountAsync(
        CustomerDiscountRule discount, 
        CancellationToken cancellationToken = default)
    {
        // Validate discount
        if (discount.DiscountType == DiscountType.Percentage && discount.DiscountValue > 100)
        {
            throw new ArgumentException("Percentage discount cannot exceed 100%");
        }

        if (discount.DiscountValue <= 0)
        {
            throw new ArgumentException("Discount value must be positive");
        }

        discount.CreatedAt = DateTime.UtcNow;
        discount.CreatedBy = _currentUserService.UserId?.ToString() ?? "System";
        discount.UsageCount = 0;

        _context.CustomerDiscountRules.Add(discount);
        await _context.SaveChangesAsync(cancellationToken);

        return discount;
    }

    public async Task<CustomerDiscountRule> UpdateDiscountAsync(
        CustomerDiscountRule discount, 
        CancellationToken cancellationToken = default)
    {
        var existing = await _context.CustomerDiscountRules
            .FirstOrDefaultAsync(d => d.Id == discount.Id, cancellationToken);

        if (existing == null)
            throw new InvalidOperationException($"Discount rule with ID {discount.Id} not found");

        // Validate discount
        if (discount.DiscountType == DiscountType.Percentage && discount.DiscountValue > 100)
        {
            throw new ArgumentException("Percentage discount cannot exceed 100%");
        }

        if (discount.DiscountValue <= 0)
        {
            throw new ArgumentException("Discount value must be positive");
        }

        existing.Name = discount.Name;
        existing.DiscountType = discount.DiscountType;
        existing.DiscountValue = discount.DiscountValue;
        existing.MinOrderAmount = discount.MinOrderAmount;
        existing.MaxOrderAmount = discount.MaxOrderAmount;
        existing.MaxUsageCount = discount.MaxUsageCount;
        existing.IsActive = discount.IsActive;
        existing.ValidFrom = discount.ValidFrom;
        existing.ValidUntil = discount.ValidUntil;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteDiscountAsync(Guid discountId, CancellationToken cancellationToken = default)
    {
        var discount = await _context.CustomerDiscountRules
            .FirstOrDefaultAsync(d => d.Id == discountId, cancellationToken);

        if (discount == null)
            throw new InvalidOperationException($"Discount rule with ID {discountId} not found");

        // Soft delete by deactivating
        discount.IsActive = false;
        discount.UpdatedAt = DateTime.UtcNow;
        discount.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);
    }

    public bool IsDiscountValid(CustomerDiscountRule discount, decimal orderAmount)
    {
        var now = DateTime.UtcNow;

        // Check if active
        if (!discount.IsActive)
            return false;

        // Check date validity
        if (discount.ValidFrom.HasValue && discount.ValidFrom.Value > now)
            return false;

        if (discount.ValidUntil.HasValue && discount.ValidUntil.Value < now)
            return false;

        // Check usage count
        if (discount.MaxUsageCount.HasValue && discount.UsageCount >= discount.MaxUsageCount.Value)
            return false;

        // Check order amount constraints
        if (discount.MinOrderAmount.HasValue && orderAmount < discount.MinOrderAmount.Value)
            return false;

        if (discount.MaxOrderAmount.HasValue && orderAmount > discount.MaxOrderAmount.Value)
            return false;

        return true;
    }

    public async Task<int> GetActiveDiscountsCountAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.CustomerDiscountRules
            .Where(d => d.IsActive &&
                       (!d.ValidFrom.HasValue || d.ValidFrom.Value <= now) &&
                       (!d.ValidUntil.HasValue || d.ValidUntil.Value >= now))
            .CountAsync(cancellationToken);
    }

    public async Task<List<CustomerDiscountRule>> GetAllDiscountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.CustomerDiscountRules
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
