using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Tasks.Queries;

// -- Get Task By Id --
public record GetTaskByIdQuery(Guid Id) : IRequest<TaskDto?>;

public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetTaskByIdQueryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var task = await _uow.Tasks.GetWithDetailsAsync(request.Id, cancellationToken);
        return task == null ? null : _mapper.Map<TaskDto>(task);
    }
}

// -- Get Tasks By Project (Paginated) --
public record GetTasksByProjectQuery(
    Guid ProjectId, int PageNumber = 1, int PageSize = 16,
    string? SearchTerm = null, string? SortBy = null,
    bool SortDescending = false, string? GroupBy = null,
    string? StatusFilter = null, string? PriorityFilter = null,
    string? AssigneeFilter = null, string? RequesterFilter = null,
    string? TagFilter = null) : IRequest<PagedResult<TaskDto>>;

public class GetTasksByProjectQueryHandler : IRequestHandler<GetTasksByProjectQuery, PagedResult<TaskDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetTasksByProjectQueryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<PagedResult<TaskDto>> Handle(GetTasksByProjectQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _uow.Tasks.GetPagedByProjectAsync(
            request.ProjectId, request.PageNumber, request.PageSize,
            request.SearchTerm, request.SortBy, request.SortDescending,
            request.GroupBy, request.StatusFilter, request.PriorityFilter,
            request.AssigneeFilter, request.RequesterFilter, request.TagFilter,
            cancellationToken);

        var result = new PagedResult<TaskDto>
        {
            Items = _mapper.Map<List<TaskDto>>(items),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };

        if (!string.IsNullOrWhiteSpace(request.GroupBy))
        {
            result.GroupedItems = request.GroupBy.ToLower() switch
            {
                "status" => result.Items.GroupBy(t => t.Status.Name)
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "assignee" => result.Items.GroupBy(t => t.AssignedToName ?? "Unassigned")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "priority" => result.Items.GroupBy(t => t.PriorityLevel.ToString())
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "epic" => result.Items.GroupBy(t => t.EpicTitle ?? "No Epic")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "sprint" => result.Items.GroupBy(t => t.SprintTitle ?? "No Sprint")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                _ => null
            };
        }

        return result;
    }
}

// -- Get Task Statuses By Project --
public record GetTaskStatusesByProjectQuery(Guid ProjectId) : IRequest<List<TaskStatusDto>>;

public class GetTaskStatusesByProjectQueryHandler : IRequestHandler<GetTaskStatusesByProjectQuery, List<TaskStatusDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetTaskStatusesByProjectQueryHandler(IUnitOfWork uow, IMapper mapper)
    { _uow = uow; _mapper = mapper; }

    public async Task<List<TaskStatusDto>> Handle(GetTaskStatusesByProjectQuery request, CancellationToken cancellationToken)
    {
        var statuses = await _uow.TaskStatuses.GetByProjectAsync(request.ProjectId, cancellationToken);
        return _mapper.Map<List<TaskStatusDto>>(statuses);
    }
}
