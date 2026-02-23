using Microsoft.EntityFrameworkCore;
using ProjectManagement.Domain.Common;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Infrastructure.Persistence.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context) { _context = context; _dbSet = context.Set<T>(); }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }
}

// -- Project Repository --
public class ProjectRepository : Repository<Project>, IProjectRepository
{
    public ProjectRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Project> Items, int TotalCount)> GetPagedAsync(
        int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        string? userId = null,
        CancellationToken ct = default)
    {
        var query = _dbSet.Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.Tasks)
            .AsQueryable();

        if (!string.IsNullOrEmpty(userId))
        {
            query = query.Where(p => p.Members.Any(m => m.UserId == userId) || p.OwnerId == userId);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(p => p.Name.Contains(searchTerm) || (p.Description != null && p.Description.Contains(searchTerm)));

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLower() switch
        {
            "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "createdat" => sortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "status" => sortDescending ? query.OrderByDescending(p => p.Status) : query.OrderBy(p => p.Status),
            _ => query.OrderByDescending(p => p.UpdatedAt)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Project?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => await _dbSet.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public async Task<Project?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.Include(p => p.Owner)
            .Include(p => p.Members).ThenInclude(m => m.User)
            .Include(p => p.Tasks)
            .Include(p => p.Epics)
            .Include(p => p.Sprints)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> IsUserMemberAsync(Guid projectId, string userId, CancellationToken ct = default)
        => await _context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId, ct);
}

// -- Task Repository --
public class TaskRepository : Repository<ProjectTask>, ITaskRepository
{
    public TaskRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<ProjectTask> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        string? groupBy = null, string? statusFilter = null,
        string? priorityFilter = null, string? assigneeFilter = null,
        string? requesterFilter = null, string? tagFilter = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(t => t.AssignedTo)
            .Include(t => t.RequestedBy)
            .Include(t => t.Epic)
            .Include(t => t.Sprint)
            .Include(t => t.Tags)
            .Where(t => t.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => t.Title.Contains(searchTerm) || (t.Description != null && t.Description.Contains(searchTerm)));

        if (!string.IsNullOrWhiteSpace(statusFilter))
            query = query.Where(t => t.Status.Name == statusFilter);

        if (!string.IsNullOrWhiteSpace(priorityFilter) && Enum.TryParse<Domain.Enums.TaskPriority>(priorityFilter, true, out var priority))
            query = query.Where(t => t.PriorityLevel == priority);

        if (!string.IsNullOrWhiteSpace(assigneeFilter))
            query = query.Where(t => t.AssignedToId == assigneeFilter);

        if (!string.IsNullOrWhiteSpace(requesterFilter))
            query = query.Where(t => t.RequestedById == requesterFilter);

        if (!string.IsNullOrWhiteSpace(tagFilter))
            query = query.Where(t => t.Tags.Any(tag => tag.Name.ToLower() == tagFilter.ToLower()));

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLower() switch
        {
            "title" => sortDescending ? query.OrderByDescending(t => t.Title) : query.OrderBy(t => t.Title),
            "priority" => sortDescending ? query.OrderByDescending(t => t.PriorityLevel) : query.OrderBy(t => t.PriorityLevel),
            "status" => sortDescending ? query.OrderByDescending(t => t.Status) : query.OrderBy(t => t.Status),
            "duedate" => sortDescending ? query.OrderByDescending(t => t.DueDate) : query.OrderBy(t => t.DueDate),
            "points" => sortDescending ? query.OrderByDescending(t => t.Points) : query.OrderBy(t => t.Points),
            _ => query.OrderByDescending(t => t.UpdatedAt)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<ProjectTask?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.AssignedTo)
            .Include(t => t.RequestedBy)
            .Include(t => t.Epic)
            .Include(t => t.Sprint)
            .Include(t => t.Tags)
            .Include(t => t.SubTasks)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<ProjectTask>> GetByEpicAsync(Guid epicId, CancellationToken ct = default)
        => await _dbSet.Where(t => t.EpicId == epicId).ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectTask>> GetBySprintAsync(Guid sprintId, CancellationToken ct = default)
        => await _dbSet.Where(t => t.SprintId == sprintId).ToListAsync(ct);

    public async Task<IReadOnlyList<ProjectTask>> GetOverdueTasksAsync(CancellationToken ct = default)
        => await _dbSet
            .Include(t => t.Project)
            .Include(t => t.Status)
            .Where(t => t.DueDate < DateTime.UtcNow && t.Status.Category != StatusCategory.Closed)
            .ToListAsync(ct);

    public async Task BulkUpdateStatusAsync(IEnumerable<Guid> ids, Guid statusId, CancellationToken ct = default)
    {
        var category = await _context.TaskStatuses
            .Where(s => s.Id == statusId)
            .Select(s => s.Category)
            .FirstOrDefaultAsync(ct);

        await _dbSet.Where(t => ids.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.StatusId, statusId)
                .SetProperty(p => p.CompletedAt, category == StatusCategory.Closed ? DateTime.UtcNow : (DateTime?)null)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task BulkUpdateAssigneeAsync(IEnumerable<Guid> ids, string assigneeId, CancellationToken ct = default)
    {
        await _dbSet.Where(t => ids.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.AssignedToId, assigneeId)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        await _dbSet.Where(t => ids.Contains(t.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task<Guid> GetDefaultStatusIdAsync(Guid projectId, CancellationToken ct = default)
    {
        return await _context.TaskStatuses
            .Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.Order)
            .Select(s => s.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Domain.Entities.TaskStatus?> GetStatusByIdAsync(Guid statusId, CancellationToken ct = default)
    {
        return await _context.TaskStatuses.FindAsync(new object[] { statusId }, ct);
    }
}

// -- Epic Repository --
public class EpicRepository : Repository<Epic>, IEpicRepository
{
    public EpicRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Epic> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        string? ownerFilter = null, string? statusFilter = null,
        string? tagFilter = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(e => e.Owner)
            .Include(e => e.Tags)
            .Where(e => e.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(e => e.Title.Contains(searchTerm));

        if (!string.IsNullOrWhiteSpace(ownerFilter))
            query = query.Where(e => e.OwnerId == ownerFilter);

        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<Domain.Enums.EpicStatus>(statusFilter, true, out var status))
            query = query.Where(e => e.Status == status);

        if (!string.IsNullOrWhiteSpace(tagFilter))
            query = query.Where(e => e.Tags.Any(tag => tag.Name.ToLower() == tagFilter.ToLower()));

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLower() switch
        {
            "title" => sortDescending ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
            "priority" => sortDescending ? query.OrderByDescending(e => e.Priority) : query.OrderBy(e => e.Priority),
            "status" => sortDescending ? query.OrderByDescending(e => e.Status) : query.OrderBy(e => e.Status),
            _ => query.OrderByDescending(e => e.UpdatedAt)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Epic?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(e => e.Owner)
            .Include(e => e.Tasks).ThenInclude(t => t.AssignedTo)
            .Include(e => e.Tasks).ThenInclude(t => t.Tags)
            .Include(e => e.Tags)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        await _dbSet.Where(e => ids.Contains(e.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task BulkUpdateStatusAsync(IEnumerable<Guid> ids, Domain.Enums.EpicStatus status, CancellationToken ct = default)
    {
        await _dbSet.Where(e => ids.Contains(e.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, status)
                .SetProperty(p => p.CompletedAt, status == Domain.Enums.EpicStatus.Done ? DateTime.UtcNow : (DateTime?)null)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }

    public async Task BulkUpdateOwnerAsync(IEnumerable<Guid> ids, string ownerId, CancellationToken ct = default)
    {
        await _dbSet.Where(e => ids.Contains(e.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.OwnerId, ownerId)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }
}

// -- Sprint Repository --
public class SprintRepository : Repository<Sprint>, ISprintRepository
{
    public SprintRepository(AppDbContext context) : base(context) { }

    public async Task<(IReadOnlyList<Sprint> Items, int TotalCount)> GetPagedByProjectAsync(
        Guid projectId, int pageNumber, int pageSize, string? searchTerm = null,
        string? sortBy = null, bool sortDescending = false,
        CancellationToken ct = default)
    {
        var query = _dbSet.Where(s => s.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(s => s.Title.Contains(searchTerm) || (s.Description != null && s.Description.Contains(searchTerm)));

        var totalCount = await query.CountAsync(ct);

        query = sortBy?.ToLower() switch
        {
            "title" => sortDescending ? query.OrderByDescending(s => s.Title) : query.OrderBy(s => s.Title),
            "startdate" => sortDescending ? query.OrderByDescending(s => s.StartsAt) : query.OrderBy(s => s.StartsAt),
            "enddate" => sortDescending ? query.OrderByDescending(s => s.EndsAt) : query.OrderBy(s => s.EndsAt),
            "state" => sortDescending ? query.OrderByDescending(s => s.State) : query.OrderBy(s => s.State),
            _ => query.OrderBy(s => s.StartsAt)
        };

        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, totalCount);
    }

    public async Task<Sprint?> GetWithDetailsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet
            .Include(s => s.Tasks).ThenInclude(t => t.AssignedTo)
            .Include(s => s.Tasks).ThenInclude(t => t.Tags)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AutoTransitionStatesAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Start sprints that should be started
        await _dbSet
            .Where(s => s.State == Domain.Enums.SprintState.Unstarted && s.StartsAt <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.State, Domain.Enums.SprintState.Started)
                .SetProperty(p => p.UpdatedAt, now), ct);

        // Complete sprints that should be done
        await _dbSet
            .Where(s => s.State == Domain.Enums.SprintState.Started && s.EndsAt <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.State, Domain.Enums.SprintState.Done)
                .SetProperty(p => p.CompletedAt, now)
                .SetProperty(p => p.UpdatedAt, now), ct);
    }

    public async Task BulkDeleteAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        await _dbSet.Where(s => ids.Contains(s.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.IsDeleted, true)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
    }
}

// -- SubTask Repository --
public class SubTaskRepository : Repository<SubTask>, ISubTaskRepository
{
    public SubTaskRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<SubTask>> GetByTaskAsync(Guid taskId, CancellationToken ct = default)
        => await _dbSet.Where(s => s.TaskId == taskId).OrderBy(s => s.CreatedAt).ToListAsync(ct);
}

// -- Comment Repository --
public class CommentRepository : Repository<Comment>, ICommentRepository
{
    public CommentRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Comment>> GetByTaskAsync(Guid taskId, CancellationToken ct = default)
        => await _dbSet.Where(c => c.TaskId == taskId).OrderBy(c => c.CreatedAt).ToListAsync(ct);
}

// -- TaskStatus Repository --
public class TaskStatusRepository : Repository<Domain.Entities.TaskStatus>, ITaskStatusRepository
{
    public TaskStatusRepository(AppDbContext context) : base(context) { }

    public async Task<IReadOnlyList<Domain.Entities.TaskStatus>> GetByProjectAsync(Guid projectId, CancellationToken ct = default)
        => await _dbSet.Where(s => s.ProjectId == projectId).OrderBy(s => s.Order).ToListAsync(ct);
}

// -- Unit of Work --
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IProjectRepository? _projects;
    private ITaskRepository? _tasks;
    private IEpicRepository? _epics;
    private ISprintRepository? _sprints;
    private ISubTaskRepository? _subTasks;
    private ICommentRepository? _comments;
    private ITaskStatusRepository? _taskStatuses;

    public UnitOfWork(AppDbContext context) { _context = context; }

    public IProjectRepository Projects => _projects ??= new ProjectRepository(_context);
    public ITaskRepository Tasks => _tasks ??= new TaskRepository(_context);
    public IEpicRepository Epics => _epics ??= new EpicRepository(_context);
    public ISprintRepository Sprints => _sprints ??= new SprintRepository(_context);
    public ISubTaskRepository SubTasks => _subTasks ??= new SubTaskRepository(_context);
    public ICommentRepository Comments => _comments ??= new CommentRepository(_context);
    public ITaskStatusRepository TaskStatuses => _taskStatuses ??= new TaskStatusRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) => await _context.SaveChangesAsync(ct);
    public void Dispose() => _context.Dispose();
}
