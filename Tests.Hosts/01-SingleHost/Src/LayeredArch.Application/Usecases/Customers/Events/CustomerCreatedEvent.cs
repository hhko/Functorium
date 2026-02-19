using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Customers;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Customers.Events;

/// <summary>
/// Customer.CreatedEvent 핸들러 - 고객 생성 로깅
/// </summary>
public sealed class CustomerCreatedEvent : IDomainEventHandler<Customer.CreatedEvent>
{
    private readonly ILogger<CustomerCreatedEvent> _logger;

    public CustomerCreatedEvent(ILogger<CustomerCreatedEvent> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Customer.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Customer created: {CustomerId}, Name: {Name}, Email: {Email}",
            notification.CustomerId,
            notification.Name,
            notification.Email);

        return ValueTask.CompletedTask;
    }
}
