namespace ProjectManagement.Domain.Enums;

public enum ProjectStatus
{
    NotStarted = 0,
    Active = 1,
    Completed = 2,
    OnHold = 3
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

public enum TaskStatus
{
    Planning = 0,
    Prioritized = 1,
    InProgress = 2,
    WaitingForThirdParty = 3,
    InQA = 4,
    Done = 5
}

public enum EpicStatus
{
    Planning = 0,
    Ready = 1,
    InProgress = 2,
    Done = 3
}

public enum SprintState
{
    Unstarted = 0,
    Started = 1,
    Done = 2
}

public enum UserRole
{
    Developer = 0,
    ProjectManager = 1,
    Admin = 2
}
