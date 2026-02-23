using MediatR;
using AutoMapper;
using ProjectManagement.Application.DTOs;
using ProjectManagement.Domain.Entities;
using ProjectManagement.Domain.Interfaces;

namespace ProjectManagement.Application.Features.Tasks.Commands;

// -- Create Task Status --
public record CreateTaskStatusCommand(
    Guid ProjectId, string Name, string Color,
    int Order, StatusCategory Category) : IRequest<TaskStatusDto>;

public class CreateTaskStatusCommandHandler : IRequestHandler<CreateTaskStatusCommand, TaskStatusDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public CreateTaskStatusCommandHandler(IUnitOfWork uow, IMapper mapper)
    { _uow = uow; _mapper = mapper; }

    public async Task<TaskStatusDto> Handle(CreateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var status = new Domain.Entities.TaskStatus
        {
            ProjectId = request.ProjectId,
            Name = request.Name,
            Color = request.Color,
            Order = request.Order,
            Category = request.Category
        };

        await _uow.TaskStatuses.AddAsync(status, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TaskStatusDto>(status);
    }
}

// -- Update Task Status --
public record UpdateTaskStatusCommand(Guid Id, string Name, string Color, int Order, StatusCategory Category) : IRequest<TaskStatusDto>;

public class UpdateTaskStatusCommandHandler : IRequestHandler<UpdateTaskStatusCommand, TaskStatusDto>
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public UpdateTaskStatusCommandHandler(IUnitOfWork uow, IMapper mapper)
    { _uow = uow; _mapper = mapper; }

    public async Task<TaskStatusDto> Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await _uow.TaskStatuses.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Status {request.Id} not found");

        status.Name = request.Name;
        status.Color = request.Color;
        status.Order = request.Order;
        status.Category = request.Category;
        status.UpdatedAt = DateTime.UtcNow;

        await _uow.TaskStatuses.UpdateAsync(status, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return _mapper.Map<TaskStatusDto>(status);
    }
}

// -- Delete Task Status --
public record DeleteTaskStatusCommand(Guid Id) : IRequest<Unit>;

public class DeleteTaskStatusCommandHandler : IRequestHandler<DeleteTaskStatusCommand, Unit>
{
    private readonly IUnitOfWork _uow;

    public DeleteTaskStatusCommandHandler(IUnitOfWork uow) { _uow = uow; }

    public async Task<Unit> Handle(DeleteTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var status = await _uow.TaskStatuses.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Status {request.Id} not found");

        // Should check if any task is using this status before deleting
        // or re-assign tasks to a default status.
        // For now, just delete.
        
        await _uow.TaskStatuses.DeleteAsync(status, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
