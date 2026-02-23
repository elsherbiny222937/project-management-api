using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Features.Sprints.Commands;
using ProjectManagement.Application.Features.Sprints.Queries;

namespace ProjectManagement.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;

    public SprintsController(IMediator mediator, IAuthorizationService authorizationService)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
    }

    [HttpGet("projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(PagedResult<SprintDto>), 200)]
    public async Task<IActionResult> GetSprints(Guid projectId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 16,
        [FromQuery] string? searchTerm = null, [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(new GetSprintsByProjectQuery(
            projectId, pageNumber, pageSize, searchTerm, sortBy, sortDescending));
        return Ok(result);
    }

    [HttpGet("sprints/{id:guid}")]
    [ProducesResponseType(typeof(SprintDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSprint(Guid id, [FromQuery] string? groupBy = null)
    {
        var result = await _mediator.Send(new GetSprintByIdQuery(id, groupBy));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(SprintDto), 201)]
    public async Task<IActionResult> CreateSprint(Guid projectId, [FromBody] CreateSprintCommand command)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(command with { ProjectId = projectId });
        return CreatedAtAction(nameof(GetSprint), new { id = result.Id }, result);
    }

    [HttpPut("sprints/{id:guid}")]
    [ProducesResponseType(typeof(SprintDto), 200)]
    public async Task<IActionResult> UpdateSprint(Guid id, [FromBody] UpdateSprintCommand command)
    {
        if (id != command.Id) return BadRequest("Route ID does not match body ID");
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("sprints/{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteSprint(Guid id)
    {
        await _mediator.Send(new DeleteSprintCommand(id));
        return NoContent();
    }

    [HttpPost("sprints/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(SprintDto), 201)]
    public async Task<IActionResult> DuplicateSprint(Guid id)
    {
        var result = await _mediator.Send(new DuplicateSprintCommand(id));
        return CreatedAtAction(nameof(GetSprint), new { id = result.Id }, result);
    }

    /// <summary>Bulk delete sprints</summary>
    [HttpDelete("sprints/bulk-delete")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkDeleteSprints([FromBody] BulkDeleteSprintsCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
