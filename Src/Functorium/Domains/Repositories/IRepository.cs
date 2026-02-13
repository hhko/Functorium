using Functorium.Applications.Observabilities;
using Functorium.Domains.Entities;

namespace Functorium.Domains.Repositories;

/// <summary>
/// Aggregate Root 단위 Repository의 공통 인터페이스.
/// 제네릭 제약을 통해 Aggregate Root 단위 영속화를 컴파일 타임에 강제합니다.
/// </summary>
/// <typeparam name="TAggregate">Aggregate Root 타입</typeparam>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
public interface IRepository<TAggregate, TId> : IAdapter
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
    /// ID로 Aggregate를 삭제합니다.
    /// </summary>
    FinT<IO, Unit> Delete(TId id);
}
