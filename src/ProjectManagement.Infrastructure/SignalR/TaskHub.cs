using Microsoft.AspNetCore.SignalR;

namespace ProjectManagement.Infrastructure.SignalR;

public class TaskHub : Hub
{
    public async Task JoinProjectGroup(string projectId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }

    public async Task LeaveProjectGroup(string projectId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project-{projectId}");
    }
}

public interface ITaskHubClient
{
    Task TaskCreated(object task);
    Task TaskUpdated(object task);
    Task TaskStatusChanged(object data);
    Task TaskDeleted(object data);
}
