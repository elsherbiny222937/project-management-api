using ProjectManagement.Domain.Common;

namespace ProjectManagement.Domain.Entities;

public enum StatusCategory
{
    Open,
    Active,
    Closed
}

public class TaskStatus : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#808080";
    public int Order { get; set; }
    public StatusCategory Category { get; set; } = StatusCategory.Open;

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;
}
