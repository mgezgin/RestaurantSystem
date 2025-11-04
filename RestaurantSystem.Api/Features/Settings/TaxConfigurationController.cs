using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Settings.Dtos;
using RestaurantSystem.Api.Features.Settings.Interfaces;
using RestaurantSystem.Domain.Entities;

namespace RestaurantSystem.Api.Features.Settings;

[ApiController]
[Route("api/[controller]")]
public class TaxConfigurationController : ControllerBase
{
    private readonly ITaxConfigurationService _taxConfigurationService;

    public TaxConfigurationController(ITaxConfigurationService taxConfigurationService)
    {
        _taxConfigurationService = taxConfigurationService;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<List<TaxConfigurationDto>>> GetAll(CancellationToken cancellationToken)
    {
        var taxConfigurations = await _taxConfigurationService.GetAllTaxConfigurationsAsync(cancellationToken);
        
        var dtos = taxConfigurations.Select(t => new TaxConfigurationDto
        {
            Id = t.Id,
            Name = t.Name,
            Rate = t.Rate,
            IsEnabled = t.IsEnabled,
            Description = t.Description
        }).ToList();

        return ApiResponse<List<TaxConfigurationDto>>.SuccessWithData(dtos);
    }

    [HttpGet("active")]
    public async Task<ApiResponse<TaxConfigurationDto?>> GetActive(CancellationToken cancellationToken)
    {
        var taxConfiguration = await _taxConfigurationService.GetActiveTaxConfigurationAsync(cancellationToken);
        
        if (taxConfiguration == null)
            return ApiResponse<TaxConfigurationDto?>.SuccessWithData(null);

        var dto = new TaxConfigurationDto
        {
            Id = taxConfiguration.Id,
            Name = taxConfiguration.Name,
            Rate = taxConfiguration.Rate,
            IsEnabled = taxConfiguration.IsEnabled,
            Description = taxConfiguration.Description
        };

        return ApiResponse<TaxConfigurationDto?>.SuccessWithData(dto);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<TaxConfigurationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var taxConfiguration = await _taxConfigurationService.GetTaxConfigurationByIdAsync(id, cancellationToken);
        
        if (taxConfiguration == null)
            return ApiResponse<TaxConfigurationDto>.Failure("Tax configuration not found");

        var dto = new TaxConfigurationDto
        {
            Id = taxConfiguration.Id,
            Name = taxConfiguration.Name,
            Rate = taxConfiguration.Rate,
            IsEnabled = taxConfiguration.IsEnabled,
            Description = taxConfiguration.Description
        };

        return ApiResponse<TaxConfigurationDto>.SuccessWithData(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<TaxConfigurationDto>> Create(
        [FromBody] CreateTaxConfigurationDto dto, 
        CancellationToken cancellationToken)
    {
        var taxConfiguration = new TaxConfiguration
        {
            Name = dto.Name,
            Rate = dto.Rate,
            IsEnabled = dto.IsEnabled,
            Description = dto.Description,
            CreatedBy = "Admin" // Will be set by service
        };

        var created = await _taxConfigurationService.CreateTaxConfigurationAsync(taxConfiguration, cancellationToken);

        var resultDto = new TaxConfigurationDto
        {
            Id = created.Id,
            Name = created.Name,
            Rate = created.Rate,
            IsEnabled = created.IsEnabled,
            Description = created.Description
        };

        return ApiResponse<TaxConfigurationDto>.SuccessWithData(resultDto, "Tax configuration created successfully");
    }

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<TaxConfigurationDto>> Update(
        [FromBody] UpdateTaxConfigurationDto dto, 
        CancellationToken cancellationToken)
    {
        var taxConfiguration = new TaxConfiguration
        {
            Id = dto.Id,
            Name = dto.Name,
            Rate = dto.Rate,
            IsEnabled = dto.IsEnabled,
            Description = dto.Description,
            CreatedBy = "System" // Will be preserved by service
        };

        try
        {
            var updated = await _taxConfigurationService.UpdateTaxConfigurationAsync(taxConfiguration, cancellationToken);

            var resultDto = new TaxConfigurationDto
            {
                Id = updated.Id,
                Name = updated.Name,
                Rate = updated.Rate,
                IsEnabled = updated.IsEnabled,
                Description = updated.Description
            };

            return ApiResponse<TaxConfigurationDto>.SuccessWithData(resultDto, "Tax configuration updated successfully");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<TaxConfigurationDto>.Failure(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResponse<bool>> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _taxConfigurationService.DeleteTaxConfigurationAsync(id, cancellationToken);
            return ApiResponse<bool>.SuccessWithData(true, "Tax configuration deleted successfully");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<bool>.Failure(ex.Message);
        }
    }
}
