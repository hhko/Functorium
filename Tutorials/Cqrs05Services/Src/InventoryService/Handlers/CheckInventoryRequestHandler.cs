using Functorium.Applications.Linq;
using Cqrs05Services.Messages;
using InventoryService.Domain;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace InventoryService.Handlers;

/// <summary>
/// 재고 확인 요청 핸들러 (Request/Reply 패턴)
/// 순수 비즈니스 로직만 처리하며, 로깅은 파이프라인에서 자동 처리됩니다.
/// LINQ 쿼리 표현식을 사용한 함수형 체이닝으로 구현됩니다.
/// </summary>
public static class CheckInventoryRequestHandler
{
    /// <summary>
    /// 재고 확인 요청 처리
    /// LINQ 쿼리 표현식을 사용하여 FinT 모나드 트랜스포머를 체이닝합니다.
    /// </summary>
    /// <param name="request">재고 확인 요청</param>
    /// <param name="repository">재고 리포지토리</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>재고 확인 응답</returns>
    public static async Task<CheckInventoryResponse> Handle(
        CheckInventoryRequest request,
        IInventoryRepository repository,
        CancellationToken cancellationToken = default)
    {
        // var ioFin = repository.GetByProductId(request.ProductId).Run();
        // var result = await Task.Run(() => ioFin.Run(), cancellationToken);
        //
        // return result.Match(
        //     Succ: item =>
        //     {
        //         var availableQuantity = item.AvailableQuantity;
        //         var isAvailable = availableQuantity >= request.Quantity;
        //
        //         return new CheckInventoryResponse(
        //             ProductId: request.ProductId,
        //             IsAvailable: isAvailable,
        //             AvailableQuantity: availableQuantity);
        //     },
        //     Fail: _ =>
        //     {
        //         return new CheckInventoryResponse(
        //             ProductId: request.ProductId,
        //             IsAvailable: false,
        //             AvailableQuantity: 0);
        //     });

        // LINQ 쿼리 표현식: Repository의 FinT<IO, InventoryItem>를 사용하여 응답 생성
        // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
        FinT<IO, CheckInventoryResponse> usecase =
            from item in repository.GetByProductId(request.ProductId)
            let availableQuantity = item.AvailableQuantity
            let isAvailable = availableQuantity >= request.Quantity
            select new CheckInventoryResponse(
                ProductId: request.ProductId,
                IsAvailable: isAvailable,
                AvailableQuantity: availableQuantity);

        // FinT<IO, CheckInventoryResponse>
        //  -Run()→           IO<Fin<CheckInventoryResponse>>
        //  -RunAsync()→      Fin<CheckInventoryResponse> (비동기 실행)
        Fin<CheckInventoryResponse> result = await usecase.Run().RunAsync();

        // Fail 케이스에서는 상품을 찾을 수 없음을 나타내는 응답 반환
        // 실제 환경에서는 파이프라인에서 예외를 처리하지만, Request/Reply 패턴이므로 응답 반환
        return result.Match(
            Succ: response => response,
            Fail: _ => new CheckInventoryResponse(
                ProductId: request.ProductId,
                IsAvailable: false,
                AvailableQuantity: 0));
    }
}

