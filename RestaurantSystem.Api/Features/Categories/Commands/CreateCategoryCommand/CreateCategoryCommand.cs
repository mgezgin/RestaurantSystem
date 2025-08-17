using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Categories.Dtos;
using RestaurantSystem.Domain.Entities;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Categories.Commands.CreateCategoryCommand;

public record CreateCategoryCommand(
    string Name,
    string? Description,
    string? ImageUrl,
    bool IsActive,
    int DisplayOrder
) : ICommand<ApiResponse<CategoryDto>>;

public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, ApiResponse<CategoryDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateCategoryCommandHandler> _logger;

    public CreateCategoryCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<CreateCategoryCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<CategoryDto>> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        // Check if category with same name exists
        var existingCategory = await _context.Categories
            .FirstOrDefaultAsync(c => c.Name == command.Name && !c.IsDeleted, cancellationToken);

        if (existingCategory != null)
        {
            return ApiResponse<CategoryDto>.Failure("Category with this name already exists");
        }

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            ImageUrl = command.ImageUrl,
            IsActive = command.IsActive,
            DisplayOrder = command.DisplayOrder,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserId?.ToString() ?? "System"
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            ProductCount = 0,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        _logger.LogInformation("Category {CategoryId} created successfully", category.Id);
        return ApiResponse<CategoryDto>.SuccessWithData(categoryDto, "Category created successfully");
    }
}
