using AutoMapper;
using MediatR;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Enums;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Epics.Commands;

// -- Create Epic --
public record CreateEpicCommand(
    Guid ProjectId, string Title, string? Description,
    int Priority = 0, string? OwnerId = null) : IRequest<EpicDto>;

public class CreateEpicCommandHandler : IRequestHandler<CreateEpicCommand, EpicDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;

    public CreateEpicCommandHandler(IUnitOfWork uow, IMapper mapper, ICurrentUserService currentUser)
    { _uow = uow; _mapper = mapper; _currentUser = currentUser; }

    public async Task<EpicDto> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = new Epic
        {
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            Status = EpicStatus.Planning,
            OwnerId = request.OwnerId ?? _currentUser.UserId,
            ProjectId = request.ProjectId
        };

        await _uow.Epics.AddAsync(epic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<EpicDto>(epic);
    }
}

// -- Update Epic --
public record UpdateEpicCommand(
    Guid Id, string Title, string? Description,
    int Priority, EpicStatus Status, string? OwnerId) : IRequest<EpicDto>;

public class UpdateEpicCommandHandler : IRequestHandler<UpdateEpicCommand, EpicDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateEpicCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<EpicDto> Handle(UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _uow.Epics.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Epic {request.Id} not found");

        epic.Title = request.Title;
        epic.Description = request.Description;
        epic.Priority = request.Priority;
        epic.Status = request.Status;
        epic.OwnerId = request.OwnerId;
        epic.UpdatedAt = DateTime.UtcNow;

        if (request.Status == EpicStatus.Done && epic.CompletedAt == null)
            epic.CompletedAt = DateTime.UtcNow;
        else if (request.Status != EpicStatus.Done)
            epic.CompletedAt = null;

        await _uow.Epics.UpdateAsync(epic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<EpicDto>(epic);
    }
}

// -- Delete Epic --
public record DeleteEpicCommand(Guid Id) : IRequest<Unit>;

public class DeleteEpicCommandHandler : IRequestHandler<DeleteEpicCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public DeleteEpicCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _uow.Epics.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Epic {request.Id} not found");

        await _uow.Epics.DeleteAsync(epic, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Duplicate Epic --
public record DuplicateEpicCommand(Guid Id) : IRequest<EpicDto>;

public class DuplicateEpicCommandHandler : IRequestHandler<DuplicateEpicCommand, EpicDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public DuplicateEpicCommandHandler(IUnitOfWork uow, IMapper mapper) { _uow = uow; _mapper = mapper; }

    public async Task<EpicDto> Handle(DuplicateEpicCommand request, CancellationToken cancellationToken)
    {
        var epic = await _uow.Epics.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Epic {request.Id} not found");

        var cloned = epic.Duplicate();
        await _uow.Epics.AddAsync(cloned, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return _mapper.Map<EpicDto>(cloned);
    }
}

// -- Bulk Update Epic Status --
public record BulkUpdateEpicStatusCommand(List<Guid> EpicIds, EpicStatus Status) : IRequest<Unit>;

public class BulkUpdateEpicStatusCommandHandler : IRequestHandler<BulkUpdateEpicStatusCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkUpdateEpicStatusCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkUpdateEpicStatusCommand request, CancellationToken cancellationToken)
    {
        await _uow.Epics.BulkUpdateStatusAsync(request.EpicIds, request.Status, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Bulk Update Epic Owner --
public record BulkUpdateEpicOwnerCommand(List<Guid> EpicIds, string OwnerId) : IRequest<Unit>;

public class BulkUpdateEpicOwnerCommandHandler : IRequestHandler<BulkUpdateEpicOwnerCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkUpdateEpicOwnerCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkUpdateEpicOwnerCommand request, CancellationToken cancellationToken)
    {
        await _uow.Epics.BulkUpdateOwnerAsync(request.EpicIds, request.OwnerId, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// -- Bulk Delete Epics --
public record BulkDeleteEpicsCommand(List<Guid> EpicIds) : IRequest<Unit>;

public class BulkDeleteEpicsCommandHandler : IRequestHandler<BulkDeleteEpicsCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public BulkDeleteEpicsCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(BulkDeleteEpicsCommand request, CancellationToken cancellationToken)
    {
        await _uow.Epics.BulkDeleteAsync(request.EpicIds, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
