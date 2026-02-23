using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Projects.Commands;

// -- Create Project --
public record CreateProjectCommand(
    string Name,
    string? Description,
    DateTime? StartDate,
    DateTime? EndDate,
    ProjectStatus Status = ProjectStatus.NotStarted,
    decimal Budget = 0) : IRequest<ProjectDto>;

public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public CreateProjectCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    {
        _uow = uow;
        _mapper = mapper;
        _currentUser = currentUser;
    }

    public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            Slug = GenerateSlug(request.Name),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status,
            Budget = request.Budget,
            OwnerId = _currentUser.UserId
        };

        await _uow.Projects.AddAsync(project, cancellationToken);

        if (_currentUser.UserId != null)
        {
            var member = new ProjectMember { ProjectId = project.Id, UserId = _currentUser.UserId };
            project.Members.Add(member);
        }

        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectDto>(project);
    }

    private static string GenerateSlug(string name) =>
        name.ToLowerInvariant().Replace(" ", "-").Replace(".", "").Replace(",", "");
}

// -- Update Project --
public record UpdateProjectCommand(
    Guid Id, string Name, string? Description,
    DateTime? StartDate, DateTime? EndDate,
    ProjectStatus Status, decimal Budget) : IRequest<ProjectDto>;

public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateProjectCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<ProjectDto> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.Id} not found");

        project.Name = request.Name;
        project.Description = request.Description;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.Budget = request.Budget;
        project.UpdatedAt = DateTime.UtcNow;

        await _uow.Projects.UpdateAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectDto>(project);
    }
}

// -- Delete Project --
public record DeleteProjectCommand(Guid Id) : IRequest<Unit>;

public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public DeleteProjectCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.Id} not found");

        await _uow.Projects.DeleteAsync(project, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Duplicate Project --
public record DuplicateProjectCommand(Guid Id) : IRequest<ProjectDto>;

public class DuplicateProjectCommandHandler : IRequestHandler<DuplicateProjectCommand, ProjectDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public DuplicateProjectCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<ProjectDto> Handle(DuplicateProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetWithDetailsAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.Id} not found");

        var cloned = project.Duplicate();
        await _uow.Projects.AddAsync(cloned, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<ProjectDto>(cloned);
    }
}

// -- Add Project Member --
public record AddProjectMemberCommand(Guid ProjectId, string UserId) : IRequest<Unit>;

public class AddProjectMemberCommandHandler : IRequestHandler<AddProjectMemberCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public AddProjectMemberCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        var project = await _uow.Projects.GetWithDetailsAsync(request.ProjectId, cancellationToken)
            ?? throw new KeyNotFoundException($"Project {request.ProjectId} not found");

        if (project.Members.Any(m => m.UserId == request.UserId))
            throw new InvalidOperationException("User is already a member of this project");

        project.Members.Add(new ProjectMember { ProjectId = request.ProjectId, UserId = request.UserId });
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
