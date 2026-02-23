using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Sprints.Commands;

// -- Create Sprint --
public record CreateSprintCommand(
    Guid ProjectId, string Title, string? Description,
    DateTime? StartsAt, DateTime? EndsAt) : IRequest<SprintDto>;

public class CreateSprintCommandHandler : IRequestHandler<CreateSprintCommand, SprintDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CreateSprintCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<SprintDto> Handle(CreateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = new Sprint
        {
            Title = request.Title,
            Description = request.Description,
            State = SprintState.Unstarted,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            ProjectId = request.ProjectId
        };

        await _uow.Sprints.AddAsync(sprint, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<SprintDto>(sprint);
    }
}

// -- Update Sprint --
public record UpdateSprintCommand(
    Guid Id, string Title, string? Description,
    SprintState State, DateTime? StartsAt, DateTime? EndsAt) : IRequest<SprintDto>;

public class UpdateSprintCommandHandler : IRequestHandler<UpdateSprintCommand, SprintDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateSprintCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<SprintDto> Handle(UpdateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _uow.Sprints.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} not found");

        sprint.Title = request.Title;
        sprint.Description = request.Description;
        sprint.State = request.State;
        sprint.StartsAt = request.StartsAt;
        sprint.EndsAt = request.EndsAt;
        sprint.UpdatedAt = DateTime.UtcNow;

        if (request.State == SprintState.Done && sprint.CompletedAt == null)
            sprint.CompletedAt = DateTime.UtcNow;

        await _uow.Sprints.UpdateAsync(sprint, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<SprintDto>(sprint);
    }
}

// -- Delete Sprint --
public record DeleteSprintCommand(Guid Id) : IRequest<Unit>;

public class DeleteSprintCommandHandler : IRequestHandler<DeleteSprintCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public DeleteSprintCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(DeleteSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _uow.Sprints.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} not found");

        await _uow.Sprints.DeleteAsync(sprint, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Duplicate Sprint --
public record DuplicateSprintCommand(Guid Id) : IRequest<SprintDto>;

public class DuplicateSprintCommandHandler : IRequestHandler<DuplicateSprintCommand, SprintDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public DuplicateSprintCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<SprintDto> Handle(DuplicateSprintCommand request, CancellationToken cancellationToken)
    {
        var sprint = await _uow.Sprints.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Sprint {request.Id} not found");

        var cloned = sprint.Duplicate();
        await _uow.Sprints.AddAsync(cloned, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<SprintDto>(cloned);
    }
}
// -- Bulk Delete Sprints --
public record BulkDeleteSprintsCommand(List<Guid> SprintIds) : IRequest<Unit>;

public class BulkDeleteSprintsCommandHandler : IRequestHandler<BulkDeleteSprintsCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkDeleteSprintsCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkDeleteSprintsCommand request, CancellationToken cancellationToken)
    {
        await _uow.Sprints.BulkDeleteAsync(request.SprintIds, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
