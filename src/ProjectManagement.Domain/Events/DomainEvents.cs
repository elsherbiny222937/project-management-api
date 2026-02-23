using ProjectManagement.Domain.Common;

namespace ProjectManagement.Domain.Events;

public class TaskStatusChangedEvent : DomainEventBase
{
    public Guid TaskId { get; }
    public Guid ProjectId { get; }
    public Guid? EpicId { get; }
    public Guid? SprintId { get; }

    public TaskStatusChangedEvent(Guid taskId, Guid projectId, Guid? epicId, Guid? sprintId)
    {
        TaskId = taskId;
        ProjectId = projectId;
        EpicId = epicId;
        SprintId = sprintId;
    }
}

public class TaskCreatedEvent : DomainEventBase
{
    public Guid TaskId { get; }
    public Guid ProjectId { get; }
    public Guid? EpicId { get; }
    public Guid? SprintId { get; }

    public TaskCreatedEvent(Guid taskId, Guid projectId, Guid? epicId = null, Guid? sprintId = null)
    {
        TaskId = taskId;
        ProjectId = projectId;
        EpicId = epicId;
        SprintId = sprintId;
    }
}

public class TaskDeletedEvent : DomainEventBase
{
    public Guid TaskId { get; }
    public Guid ProjectId { get; }
    public Guid? EpicId { get; }
    public Guid? SprintId { get; }

    public TaskDeletedEvent(Guid taskId, Guid projectId, Guid? epicId, Guid? sprintId)
    {
        TaskId = taskId;
        ProjectId = projectId;
        EpicId = epicId;
        SprintId = sprintId;
    }
}

public class UserRegisteredEvent : DomainEventBase
{
    public string UserId { get; }
    public string UserName { get; }

    public UserRegisteredEvent(string userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }
}
