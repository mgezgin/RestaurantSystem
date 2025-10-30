using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Api.Common;
using RestaurantSystem.Api.Common.Models;
using RestaurantSystem.Api.Features.Reservations.Commands.CreateTableCommand;
using RestaurantSystem.Api.Features.Reservations.Commands.DeleteTableCommand;
using RestaurantSystem.Api.Features.Reservations.Commands.UpdateTableCommand;
using RestaurantSystem.Api.Features.Reservations.Dtos;
using RestaurantSystem.Api.Features.Reservations.Queries.GetTableByIdQuery;
using RestaurantSystem.Api.Features.Reservations.Queries.GetTablesQuery;

namespace RestaurantSystem.Api.Features.Reservations;

[ApiController]
[Route("api/[controller]")]
public class TablesController : ControllerBase
{
    private readonly CustomMediator _mediator;
    private readonly ILogger<TablesController> _logger;

    public TablesController(CustomMediator mediator, ILogger<TablesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all tables
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<List<TableDto>>>> GetTables(
        [FromQuery] bool? isActive = null,
        [FromQuery] bool? isOutdoor = null)
    {
        var query = new GetTablesQuery(isActive, isOutdoor);
        var result = await _mediator.SendQuery(query);
        return Ok(result);
    }

    /// <summary>
    /// Get table by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TableDto>>> GetTable(Guid id)
    {
        var query = new GetTableByIdQuery(id);
        var result = await _mediator.SendQuery(query);

        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new table (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TableDto>>> CreateTable([FromBody] CreateTableDto tableData)
    {
        var command = new CreateTableCommand(tableData);
        var result = await _mediator.SendCommand(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetTable), new { id = result.Data!.Id }, result);
    }

    /// <summary>
    /// Update a table (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<TableDto>>> UpdateTable(Guid id, [FromBody] UpdateTableDto tableData)
    {
        var command = new UpdateTableCommand(id, tableData);
        var result = await _mediator.SendCommand(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a table (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTable(Guid id)
    {
        var command = new DeleteTableCommand(id);
        var result = await _mediator.SendCommand(command);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
