using ProjectManagement.Domain.Common;
using ProjectManagement.Domain.Enums;

namespace ProjectManagement.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.NotStarted;
    public decimal Budget { get; set; }

    public string? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
    public ICollection<Epic> Epics { get; set; } = new List<Epic>();
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    public Project Duplicate()
    {
        return new Project
        {
            Name = "Copy of " + Name,
            Description = Description,
            Slug = Slug + "-copy",
            StartDate = StartDate,
            EndDate = EndDate,
            Status = Status,
            Budget = Budget,
            OwnerId = OwnerId
        };
    }
}
