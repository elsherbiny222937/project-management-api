using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Sprints.Queries;

// -- Get Sprint By Id --
public record GetSprintByIdQuery(Guid Id, string? GroupBy = null) : IRequest<SprintDto?>;

public class GetSprintByIdQueryHandler : IRequestHandler<GetSprintByIdQuery, SprintDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetSprintByIdQueryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<SprintDto?> Handle(GetSprintByIdQuery request, CancellationToken cancellationToken)
    {
        var sprint = await _uow.Sprints.GetWithDetailsAsync(request.Id, cancellationToken);
        if (sprint == null) return null;

        var dto = _mapper.Map<SprintDto>(sprint);

        if (!string.IsNullOrWhiteSpace(request.GroupBy))
        {
            dto.GroupedTasks = request.GroupBy.ToLower() switch
            {
                "epic" => dto.Tasks.GroupBy(t => t.EpicTitle ?? "No Epic")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "state" => dto.Tasks.GroupBy(t => t.Status.Name)
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "assignee" => dto.Tasks.GroupBy(t => t.AssignedToName ?? "Unassigned")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                _ => null
            };
        }

        return dto;
    }
}

// -- Get Sprints By Project (Paginated) --
public record GetSprintsByProjectQuery(
    Guid ProjectId, int PageNumber = 1, int PageSize = 16,
    string? SearchTerm = null, string? SortBy = null,
    bool SortDescending = false) : IRequest<PagedResult<SprintDto>>;

public class GetSprintsByProjectQueryHandler : IRequestHandler<GetSprintsByProjectQuery, PagedResult<SprintDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetSprintsByProjectQueryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<PagedResult<SprintDto>> Handle(GetSprintsByProjectQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _uow.Sprints.GetPagedByProjectAsync(
            request.ProjectId, request.PageNumber, request.PageSize,
            request.SearchTerm, request.SortBy, request.SortDescending,
            cancellationToken);

        return new PagedResult<SprintDto>
        {
            Items = _mapper.Map<List<SprintDto>>(items),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
