using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Features.Projects.Commands;
using ProjectManagement.Application.Features.Projects.Queries;
using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/projects")]
[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;

    public ProjectsController(IMediator mediator, IAuthorizationService authorizationService)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
    }

    /// <summary>Get paginated, sorted, filtered list of projects</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProjectDto>), 200)]
    public async Task<IActionResult> GetProjects(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 16,
        [FromQuery] string? searchTerm = null, [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false)
    {
        var result = await _mediator.Send(new GetProjectsQuery(
            pageNumber, pageSize, searchTerm, sortBy, sortDescending));
        return Ok(result);
    }

    /// <summary>Get project by ID with full details</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProject(Guid id)
    {
        var result = await _mediator.Send(new GetProjectByIdQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new project</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDto), 201)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectCommand command)
    {
        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProject), new { id = result.Id }, result);
    }

    /// <summary>Update an existing project</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDto), 200)]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectCommand command)
    {
        if (id != command.Id) return BadRequest("Route ID does not match body ID");
        
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Delete a project</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,ProjectManager")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        await _mediator.Send(new DeleteProjectCommand(id));
        return NoContent();
    }

    /// <summary>Duplicate a project</summary>
    [HttpPost("{id:guid}/duplicate")]
    [ProducesResponseType(typeof(ProjectDto), 201)]
    public async Task<IActionResult> DuplicateProject(Guid id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(new DuplicateProjectCommand(id));
        return CreatedAtAction(nameof(GetProject), new { id = result.Id }, result);
    }

    /// <summary>Add a member to a project</summary>
    [HttpPost("{id:guid}/members")]
    [Authorize(Roles = "Admin,ProjectManager")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, id, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        await _mediator.Send(new AddProjectMemberCommand(id, request.UserId));
        return NoContent();
    }
}

public class AddMemberRequest { public string UserId { get; set; } = string.Empty; }
