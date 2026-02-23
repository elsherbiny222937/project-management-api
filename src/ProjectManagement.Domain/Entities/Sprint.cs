using ProjectManagement.Domain.Common;
using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Domain.Entities;

public class Sprint : EntityWithProgress
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SprintState State { get; set; } = SprintState.Unstarted;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

    public bool IsDone() => State == SprintState.Done;
    public bool IsStarted() => State == SprintState.Started;

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

    public Sprint Duplicate()
    {
        return new Sprint
        {
            Title = "Copy of " + Title,
            Description = Description,
            State = State,
            StartsAt = StartsAt,
            EndsAt = EndsAt,
            ProjectId = ProjectId
        };
    }
}
