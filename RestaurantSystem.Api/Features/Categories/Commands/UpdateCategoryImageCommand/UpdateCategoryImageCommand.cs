using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Api.Features.Categories.Dtos;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Categories.Commands.UpdateCategoryImageCommand;

public record UpdateCategoryImageCommand(
    Guid CategoryId,
    IFormFile Image
) : ICommand<ApiResponse<CategoryDto>>;


public class UpdateCategoryImageCommandHandler : ICommandHandler<UpdateCategoryImageCommand, ApiResponse<CategoryDto>>
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateCategoryImageCommandHandler> _logger;

    public UpdateCategoryImageCommandHandler(
        ApplicationDbContext context,
        IFileStorageService fileService,
        ICurrentUserService currentUserService,
        ILogger<UpdateCategoryImageCommandHandler> logger)
    {
        _context = context;
        _fileService = fileService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<CategoryDto>> Handle(UpdateCategoryImageCommand command, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.Id == command.CategoryId && !c.IsDeleted, cancellationToken);

        if (category == null)
        {
            return ApiResponse<CategoryDto>.Failure("Category not found");
        }

        // Validate image
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        var extension = Path.GetExtension(command.Image.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            return ApiResponse<CategoryDto>.Failure("Invalid image format. Allowed formats: jpg, jpeg, png, webp");
        }

        if (command.Image.Length > 5 * 1024 * 1024) // 5MB limit
        {
            return ApiResponse<CategoryDto>.Failure("Image size must be less than 5MB");
        }

        // Delete old image if exists
        if (!string.IsNullOrEmpty(category.ImageUrl))
        {
            await _fileService.DeleteFileAsync(category.ImageUrl);
        }

        // Upload new image
        var imageUrl = await _fileService.UploadFileAsync(command.Image, $"categories/{category.Id}");

        category.ImageUrl = imageUrl;
        category.UpdatedAt = DateTime.UtcNow;
        category.UpdatedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        var categoryDto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            ProductCount = category.ProductCategories.Count(pc => !pc.Product.IsDeleted && pc.Product.IsActive),
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };

        _logger.LogInformation("Category {CategoryId} image updated successfully", category.Id);
        return ApiResponse<CategoryDto>.SuccessWithData(categoryDto, "Category image updated successfully");
    }
}

