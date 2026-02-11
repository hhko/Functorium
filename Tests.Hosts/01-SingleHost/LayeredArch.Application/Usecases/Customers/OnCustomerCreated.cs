using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Customers;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Customers;

/// <summary>
/// Customer.CreatedEvent 핸들러 - 고객 생성 로깅
/// </summary>
public sealed class OnCustomerCreated : IDomainEventHandler<Customer.CreatedEvent>
{
    private readonly ILogger<OnCustomerCreated> _logger;

    public OnCustomerCreated(ILogger<OnCustomerCreated> logger)
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
