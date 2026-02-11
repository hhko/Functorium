using Functorium.Applications.Events;
using LayeredArch.Domain.AggregateRoots.Products;
using Microsoft.Extensions.Logging;

namespace LayeredArch.Application.Usecases.Products;

/// <summary>
/// Product.CreatedEvent 핸들러 - 상품 생성 로깅.
/// </summary>
public sealed class OnProductCreated : IDomainEventHandler<Product.CreatedEvent>
{
    private readonly ILogger<OnProductCreated> _logger;

    public OnProductCreated(ILogger<OnProductCreated> logger)
    {
        _logger = logger;
    }

    public ValueTask Handle(Product.CreatedEvent notification, CancellationToken cancellationToken)
    {
        string name = notification.Name;
        if (name.Contains("[handler-error]", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"[{nameof(OnProductCreated)}] 시뮬레이션된 핸들러 예외: 이벤트 핸들러 예외 처리 데모");
        }

        _logger.LogInformation(
            "[DomainEvent] Product created: {ProductId}, Name: {Name}, Price: {Price}",
            notification.ProductId,
            notification.Name,
            notification.Price);

        return ValueTask.CompletedTask;
    }
}
