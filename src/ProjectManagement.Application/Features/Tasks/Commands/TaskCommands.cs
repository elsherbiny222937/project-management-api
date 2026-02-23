using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Events;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Tasks.Commands;

// -- Create Task --
public record CreateTaskCommand(
    Guid ProjectId, string Title, string? Description,
    Domain.Enums.TaskPriority PriorityLevel, int Points = 0,
    Guid? StatusId = null, DateTime? DueDate = null, Guid? EpicId = null,
    Guid? SprintId = null, string? AssignedToId = null,
    List<string>? Tags = null) : IRequest<TaskDto>;

public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public CreateTaskCommandHandler(IUnitOfWork uow, IMapper mapper,
        ICurrentUserService currentUser, IMediator mediator)
    { _uow = uow; _mapper = mapper; _currentUser = currentUser; _mediator = mediator; }

    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        if (request.EpicId.HasValue)
        {
            _ = await _uow.Epics.GetByIdAsync(request.EpicId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Epic {request.EpicId} not found");
        }

        if (request.SprintId.HasValue)
        {
            _ = await _uow.Sprints.GetByIdAsync(request.SprintId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Sprint {request.SprintId} not found");
        }

        if (request.StatusId.HasValue)
        {
            _ = await _uow.TaskStatuses.GetByIdAsync(request.StatusId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Status {request.StatusId} not found");
        }

        if (!string.IsNullOrEmpty(request.AssignedToId))
        {
            var isMember = await _uow.Projects.IsUserMemberAsync(request.ProjectId, request.AssignedToId, cancellationToken);
            if (!isMember) throw new KeyNotFoundException($"User {request.AssignedToId} is not a member of project {request.ProjectId}");
        }

        var task = new ProjectTask
        {
            Title = request.Title,
            Description = request.Description,
            PriorityLevel = request.PriorityLevel,
            Points = request.Points,
            StatusId = request.StatusId ?? (await _uow.Tasks.GetDefaultStatusIdAsync(request.ProjectId, cancellationToken)),
            DueDate = request.DueDate,
            ProjectId = request.ProjectId,
            EpicId = request.EpicId,
            SprintId = request.SprintId,
            AssignedToId = request.AssignedToId,
            RequestedById = _currentUser.UserId
        };

        await _uow.Tasks.AddAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TaskCreatedEvent(task.Id, task.ProjectId, task.EpicId, task.SprintId), cancellationToken);
        return _mapper.Map<TaskDto>(task);
    }
}

// -- Update Task --
public record UpdateTaskCommand(
    Guid Id, string Title, string? Description,
    Domain.Enums.TaskPriority PriorityLevel, int Points,
    Guid StatusId, DateTime? DueDate,
    Guid? EpicId, Guid? SprintId,
    string? AssignedToId) : IRequest<TaskDto>;

public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public UpdateTaskCommandHandler(IUnitOfWork uow, IMapper mapper, IMediator mediator)
    { _uow = uow; _mapper = mapper; _mediator = mediator; }

    public async Task<TaskDto> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.Id} not found");

        var oldStatusId = task.StatusId;
        var oldEpicId = task.EpicId;
        var oldSprintId = task.SprintId;

        if (request.EpicId.HasValue && request.EpicId != oldEpicId)
        {
            _ = await _uow.Epics.GetByIdAsync(request.EpicId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Epic {request.EpicId} not found");
        }

        if (request.SprintId.HasValue && request.SprintId != oldSprintId)
        {
            _ = await _uow.Sprints.GetByIdAsync(request.SprintId.Value, cancellationToken)
                ?? throw new KeyNotFoundException($"Sprint {request.SprintId} not found");
        }

        if (request.StatusId != oldStatusId)
        {
            _ = await _uow.TaskStatuses.GetByIdAsync(request.StatusId, cancellationToken)
                ?? throw new KeyNotFoundException($"Status {request.StatusId} not found");
        }

        if (!string.IsNullOrEmpty(request.AssignedToId) && request.AssignedToId != task.AssignedToId)
        {
            var isMember = await _uow.Projects.IsUserMemberAsync(task.ProjectId, request.AssignedToId, cancellationToken);
            if (!isMember) throw new KeyNotFoundException($"User {request.AssignedToId} is not a member of project {task.ProjectId}");
        }

        task.Title = request.Title;
        task.Description = request.Description;
        task.PriorityLevel = request.PriorityLevel;
        task.Points = request.Points;
        task.StatusId = request.StatusId;
        task.DueDate = request.DueDate;
        task.EpicId = request.EpicId;
        task.SprintId = request.SprintId;
        task.AssignedToId = request.AssignedToId;
        task.UpdatedAt = DateTime.UtcNow;

        // Fetch status to check category for CompletedAt
        var status = await _uow.Tasks.GetStatusByIdAsync(request.StatusId, cancellationToken);
        if (status?.Category == StatusCategory.Closed && task.CompletedAt == null)
            task.CompletedAt = DateTime.UtcNow;
        else if (status?.Category != StatusCategory.Closed)
            task.CompletedAt = null;

        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (oldStatusId != task.StatusId || oldEpicId != task.EpicId || oldSprintId != task.SprintId)
        {
            await _mediator.Publish(new TaskStatusChangedEvent(
                task.Id, task.ProjectId, task.EpicId ?? oldEpicId, task.SprintId ?? oldSprintId), cancellationToken);
        }

        return _mapper.Map<TaskDto>(task);
    }
}

// -- Delete Task --
public record DeleteTaskCommand(Guid Id) : IRequest<Unit>;

public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public DeleteTaskCommandHandler(IUnitOfWork uow, IMediator mediator) { _uow = uow; _mediator = mediator; }

    public async Task<Unit> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.Id} not found");

        await _uow.Tasks.DeleteAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TaskDeletedEvent(
            task.Id, task.ProjectId, task.EpicId, task.SprintId), cancellationToken);

        return Unit.Value;
    }
}

// -- Duplicate Task --
public record DuplicateTaskCommand(Guid Id) : IRequest<TaskDto>;

public class DuplicateTaskCommandHandler : IRequestHandler<DuplicateTaskCommand, TaskDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public DuplicateTaskCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<TaskDto> Handle(DuplicateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.Id} not found");

        var cloned = task.Duplicate();
        await _uow.Tasks.AddAsync(cloned, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<TaskDto>(cloned);
    }
}

// -- Bulk Update Status --
public record BulkUpdateTaskStatusCommand(List<Guid> TaskIds, Guid StatusId) : IRequest<Unit>;

public class BulkUpdateTaskStatusCommandHandler : IRequestHandler<BulkUpdateTaskStatusCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkUpdateTaskStatusCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkUpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        await _uow.Tasks.BulkUpdateStatusAsync(request.TaskIds, request.StatusId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Bulk Update Assignee --
public record BulkUpdateTaskAssigneeCommand(List<Guid> TaskIds, string AssigneeId) : IRequest<Unit>;

public class BulkUpdateTaskAssigneeCommandHandler : IRequestHandler<BulkUpdateTaskAssigneeCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkUpdateTaskAssigneeCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkUpdateTaskAssigneeCommand request, CancellationToken cancellationToken)
    {
        await _uow.Tasks.BulkUpdateAssigneeAsync(request.TaskIds, request.AssigneeId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Assign Task to Sprint --
public record AssignTaskToSprintCommand(Guid TaskId, Guid SprintId) : IRequest<Unit>;

public class AssignTaskToSprintCommandHandler : IRequestHandler<AssignTaskToSprintCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public AssignTaskToSprintCommandHandler(IUnitOfWork uow, IMediator mediator) { _uow = uow; _mediator = mediator; }

    public async Task<Unit> Handle(AssignTaskToSprintCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        var oldSprintId = task.SprintId;
        task.SprintId = request.SprintId;
        task.UpdatedAt = DateTime.UtcNow;
        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TaskStatusChangedEvent(
            task.Id, task.ProjectId, task.EpicId, task.SprintId ?? oldSprintId), cancellationToken);

        return Unit.Value;
    }
}

// -- Assign Task to Epic --
public record AssignTaskToEpicCommand(Guid TaskId, Guid EpicId) : IRequest<Unit>;

public class AssignTaskToEpicCommandHandler : IRequestHandler<AssignTaskToEpicCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public AssignTaskToEpicCommandHandler(IUnitOfWork uow, IMediator mediator) { _uow = uow; _mediator = mediator; }

    public async Task<Unit> Handle(AssignTaskToEpicCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        var oldEpicId = task.EpicId;
        task.EpicId = request.EpicId;
        task.UpdatedAt = DateTime.UtcNow;
        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        await _mediator.Publish(new TaskStatusChangedEvent(
            task.Id, task.ProjectId, task.EpicId ?? oldEpicId, task.SprintId), cancellationToken);

        return Unit.Value;
    }
}

// -- Remove Task from Epic --
public record RemoveTaskFromEpicCommand(Guid TaskId) : IRequest<Unit>;

public class RemoveTaskFromEpicCommandHandler : IRequestHandler<RemoveTaskFromEpicCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public RemoveTaskFromEpicCommandHandler(IUnitOfWork uow, IMediator mediator) { _uow = uow; _mediator = mediator; }

    public async Task<Unit> Handle(RemoveTaskFromEpicCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        var oldEpicId = task.EpicId;
        task.EpicId = null;
        task.UpdatedAt = DateTime.UtcNow;
        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (oldEpicId != null)
        {
            await _mediator.Publish(new TaskStatusChangedEvent(
                task.Id, task.ProjectId, oldEpicId, task.SprintId), cancellationToken);
        }

        return Unit.Value;
    }
}

// -- Remove Task from Sprint --
public record RemoveTaskFromSprintCommand(Guid TaskId) : IRequest<Unit>;

public class RemoveTaskFromSprintCommandHandler : IRequestHandler<RemoveTaskFromSprintCommand, Unit>
{
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public RemoveTaskFromSprintCommandHandler(IUnitOfWork uow, IMediator mediator) { _uow = uow; _mediator = mediator; }

    public async Task<Unit> Handle(RemoveTaskFromSprintCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        var oldSprintId = task.SprintId;
        task.SprintId = null;
        task.UpdatedAt = DateTime.UtcNow;
        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        if (oldSprintId != null)
        {
            await _mediator.Publish(new TaskStatusChangedEvent(
                task.Id, task.ProjectId, task.EpicId, oldSprintId), cancellationToken);
        }

        return Unit.Value;
    }
}
// -- Bulk Delete Tasks --
public record BulkDeleteTasksCommand(List<Guid> TaskIds) : IRequest<Unit>;

public class BulkDeleteTasksCommandHandler : IRequestHandler<BulkDeleteTasksCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkDeleteTasksCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkDeleteTasksCommand request, CancellationToken cancellationToken)
    {
        await _uow.Tasks.BulkDeleteAsync(request.TaskIds, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
// -- Log Time --
public record LogTimeCommand(Guid TaskId, double Hours) : IRequest<TaskDto>;

public class LogTimeCommandHandler : IRequestHandler<LogTimeCommand, TaskDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public LogTimeCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<TaskDto> Handle(LogTimeCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        task.LoggedHours += request.Hours;
        task.UpdatedAt = DateTime.UtcNow;

        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TaskDto>(task);
    }
}

// -- Add Blocker --
public record AddBlockerCommand(Guid TaskId, Guid BlockerTaskId) : IRequest<Unit>;

public class AddBlockerCommandHandler : IRequestHandler<AddBlockerCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public AddBlockerCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(AddBlockerCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");
        
        var blockerTask = await _uow.Tasks.GetByIdAsync(request.BlockerTaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Blocker Task {request.BlockerTaskId} not found");

        if (task.BlockedBy.Any(b => b.Id == request.BlockerTaskId))
            return Unit.Value;

        task.BlockedBy.Add(blockerTask);
        task.UpdatedAt = DateTime.UtcNow;

        await _uow.Tasks.UpdateAsync(task, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}

// -- Remove Blocker --
public record RemoveBlockerCommand(Guid TaskId, Guid BlockerTaskId) : IRequest<Unit>;

public class RemoveBlockerCommandHandler : IRequestHandler<RemoveBlockerCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public RemoveBlockerCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(RemoveBlockerCommand request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetWithDetailsAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        var blocker = task.BlockedBy.FirstOrDefault(b => b.Id == request.BlockerTaskId);
        if (blocker != null)
        {
            task.BlockedBy.Remove(blocker);
            task.UpdatedAt = DateTime.UtcNow;
            await _uow.Tasks.UpdateAsync(task, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}

// -- Add Comment --
public record AddCommentCommand(Guid TaskId, string Content) : IRequest<CommentDto>;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public AddCommentCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    { _uow = uow; _mapper = mapper; _currentUser = currentUser; }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new Comment
        {
            TaskId = request.TaskId,
            Content = request.Content,
            UserId = _currentUser.UserId ?? throw new UnauthorizedAccessException("Must be logged in to comment")
        };

        var task = await _uow.Tasks.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found");

        task.UpdatedAt = DateTime.UtcNow;
        
        await _uow.Comments.AddAsync(comment, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        
        return _mapper.Map<CommentDto>(comment);
    }
}
