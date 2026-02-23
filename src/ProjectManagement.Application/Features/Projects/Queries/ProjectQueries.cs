using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Projects.Queries;

// -- Get Project By Id --
public record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDto?>;

public class GetProjectByIdQueryHandler : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetProjectByIdQueryHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser) 
    { _uow = uow; _mapper = mapper; _currentUser = currentUser; }

    public async Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetWithDetailsAsync(request.Id, cancellationToken);
        if (project == null) return null;

        var isAdmin = _currentUser.IsAdmin;
        if (!isAdmin && project.OwnerId != _currentUser.UserId)
        {
            var isMember = await _uow.Projects.IsUserMemberAsync(request.Id, _currentUser.UserId, cancellationToken);
            if (!isMember) return null;
        }

        return _mapper.Map<ProjectDto>(project);
    }
}

// -- Get Projects (Paginated) --
public record GetProjectsQuery(
    int PageNumber = 1, int PageSize = 16,
    string? SearchTerm = null, string? SortBy = null,
    bool SortDescending = false) : IRequest<PagedResult<ProjectDto>>;

public class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, PagedResult<ProjectDto>>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public GetProjectsQueryHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser) 
    { _uow = uow; _mapper = mapper; _currentUser = currentUser; }

    public async Task<PagedResult<ProjectDto>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var isAdmin = _currentUser.IsAdmin;
        var userId = isAdmin ? null : _currentUser.UserId;

        var (items, totalCount) = await _uow.Projects.GetPagedAsync(
            request.PageNumber, request.PageSize,
            request.SearchTerm, request.SortBy, request.SortDescending,
            userId,
            cancellationToken);

        return new PagedResult<ProjectDto>
        {
            Items = _mapper.Map<List<ProjectDto>>(items),
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
