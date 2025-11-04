using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Settings.Interfaces;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Settings.Services;

public class TaxConfigurationService : ITaxConfigurationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TaxConfigurationService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<TaxConfiguration?> GetActiveTaxConfigurationAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaxConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IsEnabled, cancellationToken);
    }

    public async Task<TaxConfiguration?> GetTaxConfigurationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaxConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<List<TaxConfiguration>> GetAllTaxConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TaxConfigurations
            .AsNoTracking()
            .OrderByDescending(t => t.IsEnabled)
            .ThenBy(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TaxConfiguration> CreateTaxConfigurationAsync(TaxConfiguration taxConfiguration, CancellationToken cancellationToken = default)
    {
        // If this tax is enabled, disable all others
        if (taxConfiguration.IsEnabled)
        {
            var existingTaxes = await _context.TaxConfigurations
                .Where(t => t.IsEnabled)
                .ToListAsync(cancellationToken);

            foreach (var tax in existingTaxes)
            {
                tax.IsEnabled = false;
                tax.UpdatedAt = DateTime.UtcNow;
                tax.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";
            }
        }

        taxConfiguration.CreatedAt = DateTime.UtcNow;
        taxConfiguration.CreatedBy = _currentUserService.UserId?.ToString() ?? "System";

        _context.TaxConfigurations.Add(taxConfiguration);
        await _context.SaveChangesAsync(cancellationToken);

        return taxConfiguration;
    }

    public async Task<TaxConfiguration> UpdateTaxConfigurationAsync(TaxConfiguration taxConfiguration, CancellationToken cancellationToken = default)
    {
        var existing = await _context.TaxConfigurations
            .FirstOrDefaultAsync(t => t.Id == taxConfiguration.Id, cancellationToken);

        if (existing == null)
            throw new InvalidOperationException($"Tax configuration with ID {taxConfiguration.Id} not found");

        // If this tax is being enabled, disable all others
        if (taxConfiguration.IsEnabled && !existing.IsEnabled)
        {
            var otherTaxes = await _context.TaxConfigurations
                .Where(t => t.Id != taxConfiguration.Id && t.IsEnabled)
                .ToListAsync(cancellationToken);

            foreach (var tax in otherTaxes)
            {
                tax.IsEnabled = false;
                tax.UpdatedAt = DateTime.UtcNow;
                tax.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";
            }
        }

        existing.Name = taxConfiguration.Name;
        existing.Rate = taxConfiguration.Rate;
        existing.IsEnabled = taxConfiguration.IsEnabled;
        existing.Description = taxConfiguration.Description;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        return existing;
    }

    public async Task DeleteTaxConfigurationAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var taxConfiguration = await _context.TaxConfigurations
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (taxConfiguration == null)
            throw new InvalidOperationException($"Tax configuration with ID {id} not found");

        _context.TaxConfigurations.Remove(taxConfiguration);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> CalculateTaxAsync(decimal amount, CancellationToken cancellationToken = default)
    {
        var activeTax = await GetActiveTaxConfigurationAsync(cancellationToken);
        
        if (activeTax == null || !activeTax.IsEnabled)
            return 0;

        return Math.Round(amount * activeTax.Rate, 2);
    }
}
