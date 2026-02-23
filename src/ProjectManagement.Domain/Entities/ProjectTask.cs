using ProjectManagement.Domain.Common;
using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Domain.Entities;

public class ProjectTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public int Points { get; set; }
    public TaskPriority PriorityLevel { get; set; } = TaskPriority.Medium;
    
    public Guid StatusId { get; set; }
    public TaskStatus Status { get; set; } = null!;

    public double? EstimatedHours { get; set; }
    public double LoggedHours { get; set; }

    public DateTime? DueDate { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid? EpicId { get; set; }
    public Epic? Epic { get; set; }

    public Guid? SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    public string? AssignedToId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }

    public string? RequestedById { get; set; }
    public ApplicationUser? RequestedBy { get; set; }

    public ICollection<SubTask> SubTasks { get; set; } = new List<SubTask>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public ICollection<ProjectTask> BlockedBy { get; set; } = new List<ProjectTask>();
    public ICollection<ProjectTask> Blocks { get; set; } = new List<ProjectTask>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public bool IsDone() => Status?.Category == StatusCategory.Closed;

    public ProjectTask Duplicate()
    {
        var cloned = new ProjectTask
        {
            Title = "Copy of " + Title,
            Description = Description,
            Priority = Priority,
            Points = Points,
            PriorityLevel = PriorityLevel,
            StatusId = StatusId,
            DueDate = DueDate,
            EstimatedHours = EstimatedHours,
            LoggedHours = LoggedHours,
            ProjectId = ProjectId,
            EpicId = EpicId,
            SprintId = SprintId,
            AssignedToId = AssignedToId,
            RequestedById = RequestedById
        };
        return cloned;
    }
}
