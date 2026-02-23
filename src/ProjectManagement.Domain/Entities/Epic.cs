using ProjectManagement.Domain.Common;
using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Domain.Entities;

public class Epic : EntityWithProgress
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Priority { get; set; }
    public EpicStatus Status { get; set; } = EpicStatus.Planning;

    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public bool IsDone() => Status == EpicStatus.Done;

    public void UpdatePointsAndProgress(IEnumerable<ProjectTask> tasks)
    {
        var taskList = tasks.ToList();
        StoryCount = taskList.Count;

        TotalPoints = taskList.Sum(t => t.Points);
        if (TotalPoints == 0)
            TotalPoints = taskList.Count;

        PointsDone = taskList.Where(t => t.IsDone()).Sum(t => t.Points);
        if (PointsDone == 0)
            PointsDone = taskList.Count(t => t.IsDone());

        Progress = TotalPoints > 0 ? (int)((float)PointsDone / TotalPoints * 100) : 0;
    }

    public void UpdateState(IEnumerable<ProjectTask> tasks)
    {
        var taskList = tasks.ToList();
        if (taskList.Count == 0) return;

        var closedCount = taskList.Count(t => t.IsDone());
        var activeCount = taskList.Count(t => t.Status?.Category == StatusCategory.Active);
        
        if (closedCount == taskList.Count)
            Status = EpicStatus.Done;
        else if (activeCount > 0 || closedCount > 0)
            Status = EpicStatus.InProgress;
        else
            Status = EpicStatus.Planning;
    }

    public Epic Duplicate()
    {
        return new Epic
        {
            Title = "Copy of " + Title,
            Description = Description,
            Priority = Priority,
            Status = Status,
            OwnerId = OwnerId,
            ProjectId = ProjectId
        };
    }
}
