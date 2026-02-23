using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Application.Features.Tasks.Commands;
using ProjectManagement.Application.Features.Tasks.Queries;

namespace ProjectManagement.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;

    public TasksController(IMediator mediator, IAuthorizationService authorizationService)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
    }

    /// <summary>Get paginated tasks for a project with filtering, sorting, and grouping</summary>
    [HttpGet("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), 200)]
    public async Task<IActionResult> GetTasks(Guid projectId,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 16,
        [FromQuery] string? searchTerm = null, [FromQuery] string? sortBy = null,
        [FromQuery] bool sortDescending = false, [FromQuery] string? groupBy = null,
        [FromQuery] string? statusFilter = null, [FromQuery] string? priorityFilter = null,
        [FromQuery] string? assigneeFilter = null, [FromQuery] string? requesterFilter = null,
        [FromQuery] string? tagFilter = null)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(new GetTasksByProjectQuery(
            projectId, pageNumber, pageSize, searchTerm, sortBy, sortDescending,
            groupBy, statusFilter, priorityFilter, assigneeFilter, requesterFilter, tagFilter));
        return Ok(result);
    }

    /// <summary>Get task by ID with subtasks and details</summary>
    [HttpGet("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetTask(Guid id)
    {
        var result = await _mediator.Send(new GetTaskByIdQuery(id));
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>Create a new task within a project</summary>
    [HttpPost("projects/{projectId:guid}/tasks")]
    [ProducesResponseType(typeof(TaskDto), 201)]
    public async Task<IActionResult> CreateTask(Guid projectId, [FromBody] CreateTaskCommand command)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(command with { ProjectId = projectId });
        return CreatedAtAction(nameof(GetTask), new { id = result.Id }, result);
    }

    /// <summary>Update an existing task</summary>
    [HttpPut("tasks/{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskCommand command)
    {
        if (id != command.Id) return BadRequest("Route ID does not match body ID");
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Delete a task</summary>
    [HttpDelete("tasks/{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteTask(Guid id)
    {
        await _mediator.Send(new DeleteTaskCommand(id));
        return NoContent();
    }

    /// <summary>Duplicate a task</summary>
    [HttpPost("tasks/{id:guid}/duplicate")]
    [ProducesResponseType(typeof(TaskDto), 201)]
    public async Task<IActionResult> DuplicateTask(Guid id)
    {
        var result = await _mediator.Send(new DuplicateTaskCommand(id));
        return CreatedAtAction(nameof(GetTask), new { id = result.Id }, result);
    }

    /// <summary>Bulk update task statuses</summary>
    [HttpPut("tasks/bulk-status-update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkUpdateStatus([FromBody] BulkUpdateTaskStatusCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>Bulk update task assignees</summary>
    [HttpPut("tasks/bulk-assignee-update")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkUpdateAssignee([FromBody] BulkUpdateTaskAssigneeCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>Bulk delete tasks</summary>
    [HttpDelete("tasks/bulk-delete")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> BulkDeleteTasks([FromBody] BulkDeleteTasksCommand command)
    {
        await _mediator.Send(command);
        return NoContent();
    }

    /// <summary>Assign task to a sprint</summary>
    [HttpPut("tasks/{id:guid}/assign-sprint")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AssignToSprint(Guid id, [FromBody] AssignSprintRequest request)
    {
        await _mediator.Send(new AssignTaskToSprintCommand(id, request.SprintId));
        return NoContent();
    }

    /// <summary>Assign task to an epic</summary>
    [HttpPut("tasks/{id:guid}/assign-epic")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AssignToEpic(Guid id, [FromBody] AssignEpicRequest request)
    {
        await _mediator.Send(new AssignTaskToEpicCommand(id, request.EpicId));
        return NoContent();
    }

    /// <summary>Remove task from its epic</summary>
    [HttpPut("tasks/{id:guid}/remove-from-epic")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveFromEpic(Guid id)
    {
        await _mediator.Send(new RemoveTaskFromEpicCommand(id));
        return NoContent();
    }

    /// <summary>Remove task from its sprint</summary>
    [HttpPut("tasks/{id:guid}/remove-from-sprint")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveFromSprint(Guid id)
    {
        await _mediator.Send(new RemoveTaskFromSprintCommand(id));
        return NoContent();
    }

    /// <summary>Log time to a task</summary>
    [HttpPost("tasks/{id:guid}/log-time")]
    [ProducesResponseType(typeof(TaskDto), 200)]
    public async Task<IActionResult> LogTime(Guid id, [FromBody] LogTimeRequest request)
    {
        var result = await _mediator.Send(new LogTimeCommand(id, request.Hours));
        return Ok(result);
    }

    /// <summary>Add a blocker to a task</summary>
    [HttpPost("tasks/{id:guid}/blockers")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> AddBlocker(Guid id, [FromBody] BlockerRequest request)
    {
        await _mediator.Send(new AddBlockerCommand(id, request.BlockerTaskId));
        return NoContent();
    }

    /// <summary>Remove a blocker from a task</summary>
    [HttpDelete("tasks/{id:guid}/blockers/{blockerTaskId:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> RemoveBlocker(Guid id, Guid blockerTaskId)
    {
        await _mediator.Send(new RemoveBlockerCommand(id, blockerTaskId));
        return NoContent();
    }

    /// <summary>Add a comment to a task</summary>
    [HttpPost("tasks/{id:guid}/comments")]
    [ProducesResponseType(typeof(CommentDto), 201)]
    public async Task<IActionResult> AddComment(Guid id, [FromBody] AddCommentRequest request)
    {
        var result = await _mediator.Send(new AddCommentCommand(id, request.Content));
        return Ok(result);
    }

    // -- Task Status Management --

    /// <summary>Get all statuses for a project</summary>
    [HttpGet("projects/{projectId:guid}/statuses")]
    [ProducesResponseType(typeof(List<TaskStatusDto>), 200)]
    public async Task<IActionResult> GetStatuses(Guid projectId)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(new ProjectManagement.Application.Features.Tasks.Queries.GetTaskStatusesByProjectQuery(projectId));
        return Ok(result);
    }

    /// <summary>Create a task status for a project</summary>
    [HttpPost("projects/{projectId:guid}/statuses")]
    [ProducesResponseType(typeof(TaskStatusDto), 201)]
    public async Task<IActionResult> CreateStatus(Guid projectId, [FromBody] CreateTaskStatusCommand command)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, projectId, "ProjectMember");
        if (!authResult.Succeeded) return Forbid();

        var result = await _mediator.Send(command with { ProjectId = projectId });
        return Ok(result);
    }

    /// <summary>Update a task status</summary>
    [HttpPut("statuses/{id:guid}")]
    [ProducesResponseType(typeof(TaskStatusDto), 200)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateTaskStatusCommand command)
    {
        if (id != command.Id) return BadRequest();
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>Delete a task status</summary>
    [HttpDelete("statuses/{id:guid}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteStatus(Guid id)
    {
        await _mediator.Send(new DeleteTaskStatusCommand(id));
        return NoContent();
    }
}

public class LogTimeRequest { public double Hours { get; set; } }
public class BlockerRequest { public Guid BlockerTaskId { get; set; } }
public class AddCommentRequest { public string Content { get; set; } = string.Empty; }

public class AssignSprintRequest { public Guid SprintId { get; set; } }
public class AssignEpicRequest { public Guid EpicId { get; set; } }
