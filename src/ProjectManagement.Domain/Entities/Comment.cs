using ProjectManagement.Domain.Common;

namespace ProjectManagement.Domain.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    
    public Guid TaskId { get; set; }
    public ProjectTask Task { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}
