using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Categories.Commands.CreateCategoryCommand;
using RestaurantSystem.Api.Features.Categories.Commands.DeleteCategoryCommand;
using RestaurantSystem.Api.Features.Categories.Commands.ReorderCategoriesCommand;
using RestaurantSystem.Api.Features.Categories.Commands.UpdateCategoryCommand;
using RestaurantSystem.Api.Features.Categories.Commands.UpdateCategoryImageCommand;
using RestaurantSystem.Api.Features.Categories.Dtos;
using RestaurantSystem.Api.Features.Categories.Queries.GetCategoriesQuery;
using RestaurantSystem.Api.Features.Categories.Queries.GetCategoryByIdQuery;
using RestaurantSystem.Api.Features.Categories.Queries.GetCategoryProductsQuery;

namespace RestaurantSystem.Api.Features.Categories;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CustomMediator _mediator;

    public CategoriesController(CustomMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all categories with optional filters
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryDto>>>> GetCategories(
        [FromQuery] GetCategoriesQuery query)
    {
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<CategoryDetailDto>>> GetCategory(Guid id)
    {
        var query = new GetCategoryByIdQuery(id);
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Get products in a category
    /// </summary>
    [HttpGet("{id}/products")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<CategoryProductDto>>>> GetCategoryProducts(
        Guid id,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isAvailable = null)
    {
        var query = new GetCategoryProductsQuery(id, pageNumber, pageSize, isActive, isAvailable);
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new category
    /// </summary>
    [HttpPost]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory(
        [FromBody] CreateCategoryCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Update a category
    /// </summary>
    [HttpPut("{id}")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(
        Guid id,
        [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse<CategoryDto>.Failure("Category ID mismatch"));
        }

        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Update category image
    /// </summary>
    [HttpPut("{id}/image")]
    [Consumes("multipart/form-data")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategoryImage(
        Guid id,
        [FromForm] IFormFile image)
    {
        var command = new UpdateCategoryImageCommand(id, image);
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Delete a category
    /// </summary>
    [HttpDelete("{id}")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<string>>> DeleteCategory(Guid id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }

    /// <summary>
    /// Reorder categories
    /// </summary>
    [HttpPut("reorder")]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<string>>> ReorderCategories(
        [FromBody] List<CategoryOrderDto> categoryOrders)
    {
        var command = new ReorderCategoriesCommand(categoryOrders);
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }
}
