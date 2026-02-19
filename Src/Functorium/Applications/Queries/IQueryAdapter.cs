using Functorium.Applications.Observabilities;
using Functorium.Domains.Specifications;

namespace Functorium.Applications.Queries;

/// <summary>
/// 비제네릭 마커 — 런타임 타입 체크, DI 스캐닝, 제네릭 제약에 활용.
/// </summary>
public interface IQueryAdapter : IAdapter { }

/// <summary>
/// 제네릭 쿼리 어댑터 — Specification 기반 검색, PagedResult 반환.
/// </summary>
public interface IQueryAdapter<TEntity, TDto> : IQueryAdapter
{
    FinT<IO, PagedResult<TDto>> Search(
        Specification<TEntity> spec,
        PageRequest page,
        SortExpression sort);
}
