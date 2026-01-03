using Functorium.Applications.Linq;
using Cqrs05Services.Messages;
using InventoryService.Domain;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace InventoryService.Handlers;

/// <summary>
/// 재고 예약 명령 핸들러 (Fire and Forget 패턴)
/// 순수 비즈니스 로직만 처리하며, 로깅은 파이프라인에서 자동 처리됩니다.
/// LINQ 쿼리 표현식을 사용한 함수형 체이닝으로 구현됩니다.
/// </summary>
public static class ReserveInventoryCommandHandler
{
    /// <summary>
    /// 재고 예약 명령 처리
    /// LINQ 쿼리 표현식을 사용하여 FinT 모나드 트랜스포머를 체이닝합니다.
    /// </summary>
    /// <param name="command">재고 예약 명령</param>
    /// <param name="repository">재고 리포지토리</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>비동기 작업</returns>
    public static async Task Handle(
        ReserveInventoryCommand command,
        IInventoryRepository repository,
        CancellationToken cancellationToken = default)
    {
        // var ioFin = repository.ReserveQuantity(command.ProductId, command.Quantity).Run();
        // var result = await Task.Run(() => ioFin.Run(), cancellationToken);
        //
        // result.Match(
        //     Succ: _ => { },
        //     Fail: error => throw new Exception(error.Message));

        // LINQ 쿼리 표현식: Repository의 FinT<IO, InventoryItem>를 사용하여 재고 예약
        // FinTUtilites.SelectMany가 FinT를 LINQ 쿼리 표현식에서 사용 가능하도록 지원
        FinT<IO, Unit> usecase =
            from _ in repository.ReserveQuantity(command.ProductId, command.Quantity)
            select unit;

        // FinT<IO, Unit>
        //  -Run()→           IO<Fin<Unit>>
        //  -RunAsync()→      Fin<Unit> (비동기 실행)
        Fin<Unit> result = await usecase.Run().RunAsync();

        // Fail 케이스에서는 예외를 던져 파이프라인에서 처리하도록 함
        result.Match(
            Succ: _ => { },
            Fail: error => throw new Exception(error.Message));
    }
}

