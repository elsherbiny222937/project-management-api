using MediatR;
using ProjectManagement.Domain.Events;
using ProjectManagement.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace ProjectManagement.Application.DomainEventHandlers;

public class TaskStatusChangedEventHandler : INotificationHandler<TaskStatusChangedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TaskStatusChangedEventHandler> _logger;
    private readonly ITaskNotificationService _notification;

    public TaskStatusChangedEventHandler(IUnitOfWork uow, ILogger<TaskStatusChangedEventHandler> logger, ITaskNotificationService notification)
    {
        _uow = uow;
        _logger = logger;
        _notification = notification;
    }

    public async Task Handle(TaskStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling TaskStatusChangedEvent for task {TaskId}", notification.TaskId);

        if (notification.EpicId.HasValue)
        {
            var epic = await _uow.Epics.GetWithDetailsAsync(notification.EpicId.Value, cancellationToken);
            if (epic != null)
            {
                epic.UpdatePointsAndProgress(epic.Tasks);
                epic.UpdateState(epic.Tasks);
                await _uow.Epics.UpdateAsync(epic, cancellationToken);
            }
        }

        if (notification.SprintId.HasValue)
        {
            var sprint = await _uow.Sprints.GetWithDetailsAsync(notification.SprintId.Value, cancellationToken);
            if (sprint != null)
            {
                sprint.UpdatePointsAndProgress(sprint.Tasks);
                await _uow.Sprints.UpdateAsync(sprint, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
        await _notification.NotifyTaskStatusChangedAsync(notification.TaskId, notification.ProjectId, notification.EpicId, notification.SprintId, cancellationToken);
    }
}

public class TaskCreatedEventHandler : INotificationHandler<TaskCreatedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ITaskNotificationService _notification;

    public TaskCreatedEventHandler(IUnitOfWork uow, ITaskNotificationService notification) 
    { _uow = uow; _notification = notification; }

    public async Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EpicId.HasValue || notification.SprintId.HasValue)
        {
            await HandleRelatedAggregates(notification.EpicId, notification.SprintId, cancellationToken);
        }
        await _notification.NotifyTaskCreatedAsync(notification.TaskId, notification.ProjectId, cancellationToken);
    }

    private async Task HandleRelatedAggregates(Guid? epicId, Guid? sprintId, CancellationToken cancellationToken)
    {
        if (epicId.HasValue)
        {
            var epic = await _uow.Epics.GetWithDetailsAsync(epicId.Value, cancellationToken);
            if (epic != null)
            {
                epic.UpdatePointsAndProgress(epic.Tasks);
                epic.UpdateState(epic.Tasks);
                await _uow.Epics.UpdateAsync(epic, cancellationToken);
            }
        }

        if (sprintId.HasValue)
        {
            var sprint = await _uow.Sprints.GetWithDetailsAsync(sprintId.Value, cancellationToken);
            if (sprint != null)
            {
                sprint.UpdatePointsAndProgress(sprint.Tasks);
                await _uow.Sprints.UpdateAsync(sprint, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }
}

public class TaskDeletedEventHandler : INotificationHandler<TaskDeletedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ITaskNotificationService _notification;

    public TaskDeletedEventHandler(IUnitOfWork uow, ITaskNotificationService notification) 
    { _uow = uow; _notification = notification; }

    public async Task Handle(TaskDeletedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.EpicId.HasValue || notification.SprintId.HasValue)
        {
            await HandleRelatedAggregates(notification.EpicId, notification.SprintId, cancellationToken);
        }
        await _notification.NotifyTaskDeletedAsync(notification.TaskId, notification.ProjectId, cancellationToken);
    }

    private async Task HandleRelatedAggregates(Guid? epicId, Guid? sprintId, CancellationToken cancellationToken)
    {
        if (epicId.HasValue)
        {
            var epic = await _uow.Epics.GetWithDetailsAsync(epicId.Value, cancellationToken);
            if (epic != null)
            {
                epic.UpdatePointsAndProgress(epic.Tasks);
                epic.UpdateState(epic.Tasks);
                await _uow.Epics.UpdateAsync(epic, cancellationToken);
            }
        }

        if (sprintId.HasValue)
        {
            var sprint = await _uow.Sprints.GetWithDetailsAsync(sprintId.Value, cancellationToken);
            if (sprint != null)
            {
                sprint.UpdatePointsAndProgress(sprint.Tasks);
                await _uow.Sprints.UpdateAsync(sprint, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }
}
