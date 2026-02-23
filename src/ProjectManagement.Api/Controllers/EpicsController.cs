using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Features.Epics.Commands;
using ProjectManagement.Application.Features.Epics.Queries;

namespace ProjectManagement.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
[Authorize]
public class EpicsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;

    public EpicsController(IMediator mediator, IAuthorizationService authorizationService)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
    }

    [HttpGet("projects/{projectId:guid}/epics")]
    [ProducesResponseType(typeof(PagedResult<EpicDto>), 200)]
    public async Task<IActionResult> GetEpics(Guid projectId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 16,
        [FromQuery] string? searchTerm = null, [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false, [FromQuery] string? ownerFilter = null,
        [FromQuery] string? statusFilter = null, [FromQuery] string? tagFilter = null)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(new GetEpicsByProjectQuery(
            projectId, pageNumber, pageSize, searchTerm, sortBy, sortDescending,
            ownerFilter, statusFilter, tagFilter));
        return Ok(result);
    }

    [HttpGet("epics/{id:guid}")]
    [ProducesResponseType(typeof(EpicDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEpic(Guid id, [FromQuery] string? groupBy = null)
    {
        var result = await _mediator.Send(new GetEpicByIdQuery(id, groupBy));
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("projects/{projectId:guid}/epics")]
    [ProducesResponseType(typeof(EpicDto), 201)]
    public async Task<IActionResult> CreateEpic(Guid projectId, [FromBody] CreateEpicCommand command)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(command with { ProjectId = projectId });
        return CreatedAtAction(nameof(GetEpic), new { id = result.Id }, result);
    }

    [HttpPut("epics/{id:guid}")]
    [ProducesResponseType(typeof(EpicDto), 200)]
    public async Task<IActionResult> UpdateEpic(Guid id, [FromBody] UpdateEpicCommand command)
    {
        if (id != command.Id) return BadRequest("Route ID does not match body ID");
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("epics/{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteEpic(Guid id)
    {
        await _mediator.Send(new DeleteEpicCommand(id));
        return NoContent();
    }

    [HttpPost("epics/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(EpicDto), 201)]
    public async Task<IActionResult> DuplicateEpic(Guid id)
    {
        var result = await _mediator.Send(new DuplicateEpicCommand(id));
        return CreatedAtAction(nameof(GetEpic), new { id = result.Id }, result);
    }

    [HttpPut("epics/bulk-status-update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateEpicStatusCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpPut("epics/bulk-owner-update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkUpdateOwner([FromBody] BulkUpdateEpicOwnerCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    [HttpDelete("epics/bulk-delete")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkDeleteEpics([FromBody] BulkDeleteEpicsCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }
}
