using FluentValidation;
using Functorium.Abstractions.Errors;
using Functorium.Applications.Cqrs;
using Functorium.Applications.Linq;
using LanguageExt;
using LanguageExt.Common;
using OrderService.Adapters.Messaging;
using CqrsObservability.Messages;
using OrderService.Domain;
using static LanguageExt.Prelude;

namespace OrderService.Usecases;

/// <summary>
/// 주문 생성 Command - 재고 확인 및 예약 플로우 데모
/// </summary>
public sealed class CreateOrderCommand
{
    /// <summary>
    /// Command Request - 주문 생성에 필요한 데이터
    /// </summary>
    public sealed record Request(
        Guid ProductId,
        int Quantity) : ICommandRequest<Response>;

    /// <summary>
    /// Command Response - 생성된 주문 정보
    /// </summary>
    public sealed record Response(
        Guid OrderId,
        Guid ProductId,
        int Quantity,
        DateTime CreatedAt);

    /// <summary>
    /// Request Validator - FluentValidation 검증 규칙
    /// </summary>
    public sealed class Validator : AbstractValidator<Request>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("상품 ID는 필수입니다");

            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("주문 수량은 0보다 커야 합니다");
        }
    }

    /// <summary>
    /// Command Handler - 실제 비즈니스 로직 구현
    /// </summary>
    internal sealed class Usecase(
        IInventoryMessaging inventoryMessaging,
        IOrderRepository orderRepository)
        : ICommandUsecase<Request, Response>
    {
        private readonly IInventoryMessaging _inventoryMessaging = inventoryMessaging;
        private readonly IOrderRepository _orderRepository = orderRepository;

        /// <summary>
        /// LINQ 쿼리 표현식을 사용한 함수형 체이닝
        /// FinTUtilites의 SelectMany 확장 메서드를 통해 FinT 모나드 트랜스포머를 LINQ로 처리
        /// 재고 확인 → 주문 생성 → 재고 예약 플로우
        /// </summary>
        public async ValueTask<FinResponse<Response>> Handle(Request request, CancellationToken cancellationToken)
        {
            // LINQ 쿼리 표현식: 재고 확인 → 주문 생성 → 재고 예약
            // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
            // guard를 사용하여 재고가 충분할 때만 계속 진행
            FinT<IO, Response> usecase =
                from checkResponse in _inventoryMessaging.CheckInventory(new CheckInventoryRequest(
                    ProductId: request.ProductId,
                    Quantity: request.Quantity))
                from _ in guard(checkResponse.IsAvailable, ApplicationErrors.InsufficientInventory(
                    request.ProductId,
                    request.Quantity,
                    checkResponse.AvailableQuantity))
                let orderId = Guid.NewGuid()
                from order in _orderRepository.Create(new Order(
                    id: orderId,
                    productId: request.ProductId,
                    quantity: request.Quantity,
                    createdAt: DateTime.UtcNow))
                from __ in _inventoryMessaging.ReserveInventory(new ReserveInventoryCommand(
                    OrderId: orderId,
                    ProductId: request.ProductId,
                    Quantity: request.Quantity))
                select new Response(
                    order.Id,
                    order.ProductId,
                    order.Quantity,
                    order.CreatedAt);

            // FinT<IO, Response>
            //  -Run()→           IO<Fin<Response>>
            //  -Run()→           Fin<Response> (동기 실행, 예외 발생 가능)
            //  -RunSafe()→       Fin<Fin<Response>> (예외를 Fin.Fail로 변환, 중첩 Fin)
            //  -Flatten()→       Fin<Response> (중첩 Fin 제거)
            //  -ToFinResponse()→ FinResponse<Response>
            var ioFin = usecase.Run();
            Fin<Response> response = ioFin.RunSafe().Flatten();
            return response.ToFinResponse();
        }
    }

    /// <summary>
    /// ApplicationErrors 중첩 클래스 - Application 계층 오류 정의
    /// DomainErrors 패턴과 동일한 구조로 오류를 정의하여 일관성 유지
    /// </summary>
    internal static class ApplicationErrors
    {
        /// <summary>
        /// 재고가 부족한 경우 발생하는 오류
        /// </summary>
        public static Error InsufficientInventory(Guid productId, int requestedQuantity, int availableQuantity) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(ApplicationErrors)}.{nameof(CreateOrderCommand)}.{nameof(InsufficientInventory)}",
                errorCurrentValue: requestedQuantity,
                errorMessage: $"재고가 부족합니다. 상품 ID: '{productId}', 요청 수량: {requestedQuantity}, 사용 가능한 수량: {availableQuantity}");
    }
}

