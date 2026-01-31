using Functorium.Applications.Events;
using LayeredArch.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.CreatedEvent 핸들러 - 상품 생성 로깅.
/// </summary>
public sealed class OnProductCreatedHandler : IDomainEventHandler<Product.CreatedEvent>
{
    private readonly ILogger<OnProductCreatedHandler> _logger;

    public OnProductCreatedHandler(ILogger<OnProductCreatedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.CreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
            notification.ProductId,
            notification.Name,
            notification.Price);

        return ValueTask.CompletedTask;
    }
}
