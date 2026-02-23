using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Epics.Queries;

// -- Get Epic By Id --
public record GetEpicByIdQuery(Guid Id, string? GroupBy = null) : IRequest<EpicDto?>;

public class GetEpicByIdQueryHandler : IRequestHandler<GetEpicByIdQuery, EpicDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetEpicByIdQueryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<EpicDto?> Handle(GetEpicByIdQuery request, CancellationToken cancellationToken)
    {
        var epic = await _uow.Epics.GetWithDetailsAsync(request.Id, cancellationToken);
        if (epic == null) return null;

        var dto = _mapper.Map<EpicDto>(epic);

        if (!string.IsNullOrWhiteSpace(request.GroupBy))
        {
            dto.GroupedTasks = request.GroupBy.ToLower() switch
            {
                "sprint" => dto.Tasks.GroupBy(t => t.SprintTitle ?? "No Sprint")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "state" => dto.Tasks.GroupBy(t => t.Status.Name)
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "requester" => dto.Tasks.GroupBy(t => t.RequestedByName ?? "Unset")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                "assignee" => dto.Tasks.GroupBy(t => t.AssignedToName ?? "Unassigned")
                    .Select(g => new GroupedTaskDto { GroupName = g.Key, Tasks = g.ToList() }).ToList(),
                _ => null
            };
        }

        return dto;
    }
}

// -- Get Epics By Project (Paginated) --
public record GetEpicsByProjectQuery(
    Guid ProjectId, int PageNumber = 1, int PageSize = 16,
    string? SearchTerm = null, string? SortBy = null,
    bool SortDescending = false, string? OwnerFilter = null,
    string? StatusFilter = null, string? TagFilter = null) : IRequest<PagedResult<EpicDto>>;

public class GetEpicsByProjectQueryHandler : IRequestHandler<GetEpicsByProjectQuery, PagedResult<EpicDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public GetEpicsByProjectQueryHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<PagedResult<EpicDto>> Handle(GetEpicsByProjectQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _uow.Epics.GetPagedByProjectAsync(
            request.ProjectId, request.PageNumber, request.PageSize,
            request.SearchTerm, request.SortBy, request.SortDescending,
            request.OwnerFilter, request.StatusFilter, request.TagFilter,
            cancellationToken);

        return new PagedResult<EpicDto>
        {
            Items = _mapper.Map<List<EpicDto>>(items),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
