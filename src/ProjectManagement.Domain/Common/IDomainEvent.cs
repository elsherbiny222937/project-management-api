using MediatR;

namespace ProjectManagement.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}

public abstract class DomainEventBase : IDomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
