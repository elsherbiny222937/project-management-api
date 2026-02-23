using Microsoft.AspNetCore.SignalR;
using ProjectManagement.Domain.Interfaces;
using ProjectManagement.Infrastructure.SignalR;

namespace ProjectManagement.Infrastructure.Services;

public class TaskNotificationService : ITaskNotificationService
{
    private readonly IHubContext<TaskHub> _hubContext;

    public TaskNotificationService(IHubContext<TaskHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTaskCreatedAsync(Guid taskId, Guid projectId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"project-{projectId}")
            .SendAsync("TaskCreated", new { taskId, projectId }, ct);
    }

    public async Task NotifyTaskUpdatedAsync(Guid taskId, Guid projectId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"project-{projectId}")
            .SendAsync("TaskUpdated", new { taskId, projectId }, ct);
    }

    public async Task NotifyTaskDeletedAsync(Guid taskId, Guid projectId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"project-{projectId}")
            .SendAsync("TaskDeleted", new { taskId, projectId }, ct);
    }

    public async Task NotifyTaskStatusChangedAsync(Guid taskId, Guid projectId, Guid? epicId, Guid? sprintId, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"project-{projectId}")
            .SendAsync("TaskStatusChanged", new { taskId, projectId, epicId, sprintId }, ct);
    }
}
