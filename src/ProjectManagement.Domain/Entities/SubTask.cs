using ProjectManagement.Domain.Common;

namespace ProjectManagement.Domain.Entities;

public class SubTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public SubTask Duplicate(ProjectTask? parent = null)
    {
        return new SubTask
        {
            Title = Title,
            Description = Description,
            TaskId = parent?.Id ?? TaskId
        };
    }
}
