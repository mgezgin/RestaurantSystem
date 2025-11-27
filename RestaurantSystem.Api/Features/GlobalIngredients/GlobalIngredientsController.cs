using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.GlobalIngredients.Dtos;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.GlobalIngredients;

[ApiController]
[Route("api/global-ingredients")]
public class GlobalIngredientsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public GlobalIngredientsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get all global ingredients
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<GlobalIngredientDto>>>> GetGlobalIngredients()
    {
        var ingredients = await _context.GlobalIngredients
            .Include(g => g.Translations)
            .Where(g => g.IsActive)
            .OrderBy(g => g.DefaultName)
            .ToListAsync();

        var dtos = ingredients.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<GlobalIngredientDto>>.SuccessWithData(dtos));
    }

    /// <summary>
    /// Get global ingredient by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<GlobalIngredientDto>>> GetGlobalIngredient(Guid id)
    {
        var ingredient = await _context.GlobalIngredients
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (ingredient == null)
        {
            return NotFound(ApiResponse<GlobalIngredientDto>.Failure("Global ingredient not found"));
        }

        return Ok(ApiResponse<GlobalIngredientDto>.SuccessWithData(MapToDto(ingredient)));
    }

    /// <summary>
    /// Search global ingredients by name (for autocomplete)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<GlobalIngredientDto>>>> SearchIngredients(
        [FromQuery] string query,
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Ok(ApiResponse<List<GlobalIngredientDto>>.SuccessWithData(new List<GlobalIngredientDto>()));
        }

        var normalizedQuery = query.Trim().ToLower();

        var ingredients = await _context.GlobalIngredients
            .Include(g => g.Translations)
            .Where(g => g.IsActive && g.DefaultName.ToLower().Contains(normalizedQuery))
            .OrderBy(g => g.DefaultName.ToLower().StartsWith(normalizedQuery) ? 0 : 1) // Prioritize starts-with matches
            .ThenBy(g => g.DefaultName)
            .Take(limit)
            .ToListAsync();

        var dtos = ingredients.Select(MapToDto).ToList();
        return Ok(ApiResponse<List<GlobalIngredientDto>>.SuccessWithData(dtos));
    }

    /// <summary>
    /// Create a new global ingredient
    /// </summary>
    [HttpPost]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<GlobalIngredientDto>>> CreateGlobalIngredient([FromBody] CreateGlobalIngredientDto command)
    {
        var ingredient = new GlobalIngredient
        {
            DefaultName = command.DefaultName,
            ImageUrl = command.ImageUrl,
            IsActive = true,
            CreatedBy = "System", // Required by BaseEntity, will be overwritten by DbContext
            Translations = command.Translations.Select(t => new GlobalIngredientTranslation
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name,
                CreatedBy = "System" // Required by BaseEntity
            }).ToList()
        };

        _context.GlobalIngredients.Add(ingredient);
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<GlobalIngredientDto>.SuccessWithData(MapToDto(ingredient)));
    }

    /// <summary>
    /// Update a global ingredient
    /// </summary>
    [HttpPut("{id}")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<GlobalIngredientDto>>> UpdateGlobalIngredient(Guid id, [FromBody] UpdateGlobalIngredientDto command)
    {
        var ingredient = await _context.GlobalIngredients
            .Include(g => g.Translations)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (ingredient == null)
        {
            return NotFound(ApiResponse<GlobalIngredientDto>.Failure("Global ingredient not found"));
        }

        ingredient.DefaultName = command.DefaultName;
        ingredient.ImageUrl = command.ImageUrl;
        ingredient.IsActive = command.IsActive;

        // Update translations
        // Remove existing translations that are not in the command
        var translationsToRemove = ingredient.Translations
            .Where(t => !command.Translations.Any(ct => ct.LanguageCode == t.LanguageCode))
            .ToList();

        foreach (var translation in translationsToRemove)
        {
            _context.GlobalIngredientTranslations.Remove(translation);
        }

        // Add or update translations
        foreach (var translationDto in command.Translations)
        {
            var existingTranslation = ingredient.Translations
                .FirstOrDefault(t => t.LanguageCode == translationDto.LanguageCode);

            if (existingTranslation != null)
            {
                existingTranslation.Name = translationDto.Name;
            }
            else
            {
                ingredient.Translations.Add(new GlobalIngredientTranslation
                {
                    LanguageCode = translationDto.LanguageCode,
                    Name = translationDto.Name,
                    CreatedBy = "System" // Required by BaseEntity
                });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(ApiResponse<GlobalIngredientDto>.SuccessWithData(MapToDto(ingredient)));
    }

    /// <summary>
    /// Delete a global ingredient
    /// </summary>
    [HttpDelete("{id}")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<string>>> DeleteGlobalIngredient(Guid id)
    {
        var ingredient = await _context.GlobalIngredients.FindAsync(id);

        if (ingredient == null)
        {
            return NotFound(ApiResponse<string>.Failure("Global ingredient not found"));
        }

        _context.GlobalIngredients.Remove(ingredient); // Soft delete handled by entity type
        await _context.SaveChangesAsync();

        return Ok(ApiResponse<string>.SuccessWithData("Global ingredient deleted successfully"));
    }

    private static GlobalIngredientDto MapToDto(GlobalIngredient ingredient)
    {
        return new GlobalIngredientDto
        {
            Id = ingredient.Id,
            DefaultName = ingredient.DefaultName,
            ImageUrl = ingredient.ImageUrl,
            IsActive = ingredient.IsActive,
            Translations = ingredient.Translations.Select(t => new GlobalIngredientTranslationDto
            {
                LanguageCode = t.LanguageCode,
                Name = t.Name
            }).ToList()
        };
    }
}
