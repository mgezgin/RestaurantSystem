using Microsoft.EntityFrameworkCore;
using RestaurantSystem.Api.Abstraction.Messaging;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Common.Services.Interfaces;
using RestaurantSystem.Infrastructure.Persistence;

namespace RestaurantSystem.Api.Features.Products.Commands.DeleteProductCommand;

public record DeleteProductCommand(Guid Id) : ICommand<ApiResponse<string>>;


public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, ApiResponse<string>>
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ApiResponse<string>> Handle(DeleteProductCommand command, CancellationToken cancellationToken)
    {

        var category = await _context.Products
            .Include(c => c.ProductCategories)
            .FirstOrDefaultAsync(c => c.Id == command.Id && !c.IsDeleted, cancellationToken);

        if (category == null)
        {
            return ApiResponse<string>.Failure("Category not found");
        }

        // Check if category has products
        if (category.ProductCategories.Any(pc => !pc.Product.IsDeleted))
        {
            return ApiResponse<string>.Failure("Cannot delete category with associated products. Please remove all products from this category first.");
        }

        // Soft delete
        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        category.DeletedBy = _currentUserService.UserId?.ToString() ?? "System";

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Category {CategoryId} deleted successfully", category.Id);
        return ApiResponse<string>.SuccessWithData("Category deleted successfully");
    }
}