using ProjectManagement.Domain.Common;

namespace ProjectManagement.Domain.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

public interface IProjectRepository : IRepository<Entities.Project>
{
    Task<(IReadOnlyList<Entities.Project> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        string? userId = null,
        CancellationToken cancellationToken = default);
    Task<Entities.Project?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Entities.Project?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> IsUserMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken = default);
}

public interface ITaskRepository : IRepository<Entities.ProjectTask>
{
    Task<(IReadOnlyList<Entities.ProjectTask> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        string? groupBy = null, string? statusFilter = null,
        string? priorityFilter = null, string? assigneeFilter = null,
        string? requesterFilter = null, string? tagFilter = null,
        CancellationToken cancellationToken = default);
    Task<Entities.ProjectTask?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.ProjectTask>> GetByEpicAsync(Guid epicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.ProjectTask>> GetBySprintAsync(Guid sprintId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.ProjectTask>> GetOverdueTasksAsync(CancellationToken cancellationToken = default);
    Task BulkUpdateStatusAsync(IEnumerable<Guid> ids, Guid statusId, CancellationToken cancellationToken = default);
    Task BulkUpdateAssigneeAsync(IEnumerable<Guid> ids, string assigneeId, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task<Guid> GetDefaultStatusIdAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Entities.TaskStatus?> GetStatusByIdAsync(Guid statusId, CancellationToken cancellationToken = default);
}

public interface IEpicRepository : IRepository<Entities.Epic>
{
    Task<(IReadOnlyList<Entities.Epic> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        string? ownerFilter = null, string? statusFilter = null,
        string? tagFilter = null,
        CancellationToken cancellationToken = default);
    Task<Entities.Epic?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task BulkUpdateStatusAsync(IEnumerable<Guid> ids, Enums.EpicStatus status, CancellationToken cancellationToken = default);
    Task BulkUpdateOwnerAsync(IEnumerable<Guid> ids, string ownerId, CancellationToken cancellationToken = default);
}

public interface ISprintRepository : IRepository<Entities.Sprint>
{
    Task<(IReadOnlyList<Entities.Sprint> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        CancellationToken cancellationToken = default);
    Task<Entities.Sprint?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task AutoTransitionStatesAsync(CancellationToken cancellationToken = default);
    Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}

public interface ISubTaskRepository : IRepository<Entities.SubTask>
{
    Task<IReadOnlyList<Entities.SubTask>> GetByTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}

public interface ICommentRepository : IRepository<Entities.Comment>
{
    Task<IReadOnlyList<Entities.Comment>> GetByTaskAsync(Guid taskId, CancellationToken cancellationToken = default);
}

public interface ITaskStatusRepository : IRepository<Entities.TaskStatus>
{
    Task<IReadOnlyList<Entities.TaskStatus>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
}

public interface IUnitOfWork : IDisposable
{
    IProjectRepository Projects { get; }
    ITaskRepository Tasks { get; }
    IEpicRepository Epics { get; }
    ISprintRepository Sprints { get; }
    ISubTaskRepository SubTasks { get; }
    ICommentRepository Comments { get; }
    ITaskStatusRepository TaskStatuses { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
