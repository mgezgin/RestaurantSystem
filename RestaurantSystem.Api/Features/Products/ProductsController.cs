using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Authorization;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Products.Commands.CreateProductCommand;
using RestaurantSystem.Api.Features.Products.Commands.GetProductByIdQuery;
using RestaurantSystem.Api.Features.Products.Commands.GetProductsQuery;
using RestaurantSystem.Api.Features.Products.Dtos;
using RestaurantSystem.Domain.Common.Enums;

namespace RestaurantSystem.Api.Features.Products;
[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly CustomMediator _mediator;

    public ProductsController(CustomMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all products with optional filters
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<PagedResult<ProductSummaryDto>>>> GetProducts(
        [FromQuery] GetProductsQuery query)
    {
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProduct(Guid id)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [RequireAdmin]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _mediator.SendCommand(command);
        return Ok(result);
    }


}
