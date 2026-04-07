using Functorium.Abstractions.Observabilities;
using Functorium.Domains.Entities;

namespace Functorium.Domains.Repositories;

/// <summary>
/// Aggregate Root 단위 Repository의 공통 인터페이스.
/// 제네릭 제약을 통해 Aggregate Root 단위 영속화를 컴파일 타임에 강제합니다.
/// </summary>
/// <typeparam name="TAggregate">Aggregate Root 타입</typeparam>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    /// <summary>
    /// Aggregate를 생성합니다.
    /// </summary>
    FinT<IO, TAggregate> Create(TAggregate aggregate);

    /// <summary>
    /// ID로 Aggregate를 조회합니다.
    /// </summary>
    FinT<IO, TAggregate> GetById(TId id);

    /// <summary>
    /// Aggregate를 업데이트합니다.
    /// </summary>
    FinT<IO, TAggregate> Update(TAggregate aggregate);

    /// <summary>
    /// ID로 Aggregate를 삭제합니다. 삭제된 건수를 반환합니다.
    /// </summary>
    FinT<IO, int> Delete(TId id);

    /// <summary>
    /// 여러 Aggregate를 일괄 생성합니다.
    /// </summary>
    FinT<IO, Seq<TAggregate>> CreateRange(IReadOnlyList<TAggregate> aggregates);

    /// <summary>
    /// 여러 ID로 Aggregate를 일괄 조회합니다.
    /// </summary>
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);

    /// <summary>
    /// 여러 Aggregate를 일괄 업데이트합니다.
    /// </summary>
    FinT<IO, Seq<TAggregate>> UpdateRange(IReadOnlyList<TAggregate> aggregates);

    /// <summary>
    /// 여러 ID로 Aggregate를 일괄 삭제합니다. 삭제된 건수를 반환합니다.
    /// </summary>
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);
}
