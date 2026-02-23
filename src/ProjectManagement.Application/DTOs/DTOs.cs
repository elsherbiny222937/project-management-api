using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Application.DTOs;

public class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; }
    public decimal Budget { get; set; }
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int TaskCount { get; set; }
    public int MemberCount { get; set; }
    public List<ProjectMemberDto> Members { get; set; } = new();
}

public class ProjectMemberDto
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class TaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public int Points { get; set; }
    public TaskPriority PriorityLevel { get; set; }
    public TaskStatusDto Status { get; set; } = null!;
    public double? EstimatedHours { get; set; }
    public double LoggedHours { get; set; }
    public DateTime? DueDate { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? EpicId { get; set; }
    public string? EpicTitle { get; set; }
    public Guid? SprintId { get; set; }
    public string? SprintTitle { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public string? RequestedById { get; set; }
    public string? RequestedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<SubTaskDto> SubTasks { get; set; } = new();
    public List<CommentDto> Comments { get; set; } = new();
    public List<Guid> BlockedByTaskIds { get; set; } = new();
    public List<Guid> BlocksTaskIds { get; set; } = new();
}

public class TaskStatusDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Order { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class CommentDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class EpicDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public EpicStatus Status { get; set; }
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public Guid ProjectId { get; set; }
    public int TotalPoints { get; set; }
    public int StoryCount { get; set; }
    public int PointsDone { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public List<TaskDto> Tasks { get; set; } = new();
    public List<GroupedTaskDto>? GroupedTasks { get; set; }
}

public class SprintDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SprintState State { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
    public Guid ProjectId { get; set; }
    public int TotalPoints { get; set; }
    public int StoryCount { get; set; }
    public int PointsDone { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<TaskDto> Tasks { get; set; } = new();
    public List<GroupedTaskDto>? GroupedTasks { get; set; }
}

public class SubTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

public class AuthResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserDto User { get; set; } = null!;
}

public class GroupedTaskDto
{
    public string GroupName { get; set; } = string.Empty;
    public List<TaskDto> Tasks { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public List<GroupedTaskDto>? GroupedItems { get; set; }
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
