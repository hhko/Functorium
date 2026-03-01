using Functorium.Domains.Observabilities;
using Functorium.Domains.Specifications;

namespace Functorium.Applications.Queries;

/// <summary>
/// 비제네릭 마커 — 런타임 타입 체크, DI 스캐닝, 제네릭 제약에 활용.
/// </summary>
public interface IQueryPort : IObservablePort { }

/// <summary>
/// 제네릭 쿼리 어댑터 — Specification 기반 검색, PagedResult 반환.
/// </summary>
public interface IQueryPort<TEntity, TDto> : IQueryPort
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);

    /// <summary>
    /// Keyset(Cursor) 기반 페이지네이션 검색.
    /// deep page에서 O(1) 성능을 제공합니다.
    /// </summary>
    FinT<IO, CursorPagedResult<TDto>> SearchByCursor(
        Specification<TEntity> spec,
        CursorPageRequest cursor,
        SortExpression sort);

    /// <summary>
    /// Specification 기반 스트리밍 조회. 대량 데이터를 메모리에 전체 적재하지 않고 yield합니다.
    /// </summary>
    IAsyncEnumerable<TDto> Stream(
        Specification<TEntity> spec,
        SortExpression sort,
        CancellationToken cancellationToken = default);
}
