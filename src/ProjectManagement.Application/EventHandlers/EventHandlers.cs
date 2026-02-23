using MediatR;
using Microsoft.Extensions.Logging;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Events;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.EventHandlers;

// Recalculates Epic & Sprint progress when a task status changes
public class TaskStatusChangedEventHandler : INotificationHandler<TaskStatusChangedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TaskStatusChangedEventHandler> _logger;
    private readonly ITaskNotificationService _notificationService;

    public TaskStatusChangedEventHandler(IUnitOfWork uow, ILogger<TaskStatusChangedEventHandler> logger, ITaskNotificationService notificationService)
    { 
        _uow = uow; 
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TaskStatusChangedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} status changed, recalculating parents", notification.TaskId);

        if (notification.EpicId.HasValue)
        {
            var epic = await _uow.Epics.GetWithDetailsAsync(notification.EpicId.Value, cancellationToken);
            if (epic != null)
            {
                var tasks = await _uow.Tasks.GetByEpicAsync(epic.Id, cancellationToken);
                epic.UpdatePointsAndProgress(tasks);
                epic.UpdateState(tasks);
                await _uow.Epics.UpdateAsync(epic, cancellationToken);
            }
        }

        if (notification.SprintId.HasValue)
        {
            var sprint = await _uow.Sprints.GetWithDetailsAsync(notification.SprintId.Value, cancellationToken);
            if (sprint != null)
            {
                var tasks = await _uow.Tasks.GetBySprintAsync(sprint.Id, cancellationToken);
                sprint.UpdatePointsAndProgress(tasks);
                await _uow.Sprints.UpdateAsync(sprint, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Notify via real-time service
        await _notificationService.NotifyTaskStatusChangedAsync(
            notification.TaskId, notification.ProjectId, notification.EpicId, notification.SprintId, cancellationToken);
    }
}

// Handles task creation
public class TaskCreatedEventHandler : INotificationHandler<TaskCreatedEvent>
{
    private readonly ITaskNotificationService _notificationService;
    private readonly ILogger<TaskCreatedEventHandler> _logger;

    public TaskCreatedEventHandler(ITaskNotificationService notificationService, ILogger<TaskCreatedEventHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Handle(TaskCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} created", notification.TaskId);
        await _notificationService.NotifyTaskCreatedAsync(notification.TaskId, notification.ProjectId, cancellationToken);
    }
}

// Handles task deletion — recalculates parent progress
public class TaskDeletedEventHandler : INotificationHandler<TaskDeletedEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<TaskDeletedEventHandler> _logger;
    private readonly ITaskNotificationService _notificationService;

    public TaskDeletedEventHandler(IUnitOfWork uow, ILogger<TaskDeletedEventHandler> logger, ITaskNotificationService notificationService)
    { 
        _uow = uow; 
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(TaskDeletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task {TaskId} deleted, recalculating parents", notification.TaskId);

        if (notification.EpicId.HasValue)
        {
            var epic = await _uow.Epics.GetByIdAsync(notification.EpicId.Value, cancellationToken);
            if (epic != null)
            {
                var tasks = await _uow.Tasks.GetByEpicAsync(epic.Id, cancellationToken);
                epic.UpdatePointsAndProgress(tasks);
                await _uow.Epics.UpdateAsync(epic, cancellationToken);
            }
        }

        if (notification.SprintId.HasValue)
        {
            var sprint = await _uow.Sprints.GetByIdAsync(notification.SprintId.Value, cancellationToken);
            if (sprint != null)
            {
                var tasks = await _uow.Tasks.GetBySprintAsync(sprint.Id, cancellationToken);
                sprint.UpdatePointsAndProgress(tasks);
                await _uow.Sprints.UpdateAsync(sprint, cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        // Notify via real-time service
        await _notificationService.NotifyTaskDeletedAsync(notification.TaskId, notification.ProjectId, cancellationToken);
    }
}

// Creates default project on user registration (maps from create_default_workspace signal)
public class UserRegisteredEventHandler : INotificationHandler<UserRegisteredEvent>
{
    private readonly IUnitOfWork _uow;
    private readonly ILogger<UserRegisteredEventHandler> _logger;

    public UserRegisteredEventHandler(IUnitOfWork uow, ILogger<UserRegisteredEventHandler> logger)
    { _uow = uow; _logger = logger; }

    public async Task Handle(UserRegisteredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating default project for new user {UserName}", notification.UserName);

        var defaultProject = new Project
        {
            Name = $"{notification.UserName}'s Project",
            Description = "Your default project",
            Slug = notification.UserName.ToLowerInvariant().Replace(" ", "-").Replace(".", "").Replace(",", ""),
            OwnerId = notification.UserId
        };

        defaultProject.Members.Add(new ProjectMember
        {
            ProjectId = defaultProject.Id,
            UserId = notification.UserId
        });

        await _uow.Projects.AddAsync(defaultProject, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }
}
