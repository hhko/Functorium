using Functorium.Abstractions.Observabilities;
using Functorium.Domains.Entities;
using Functorium.Domains.Specifications;

namespace Functorium.Domains.Repositories;

/// <summary>
/// Aggregate Root 단위 Repository의 공통 인터페이스.
/// 제네릭 제약을 통해 Aggregate Root 단위 영속화를 컴파일 타임에 강제합니다.
/// </summary>
/// <remarks>
/// <para><b>Write Single</b> — 단건 CRUD. Update는 LINQ 모나드 합성을 위해 TAggregate를 반환합니다.</para>
/// <para><b>Write Batch</b> — 벌크 쓰기. 호출자가 이미 aggregate를 보유하므로 영향 받은 건수(int)를 반환합니다.</para>
/// <para><b>Read</b> — ID 기반 조회.</para>
/// <para><b>Specification</b> — 조건 기반 집계/삭제. PropertyMap 설정이 필요합니다.</para>
/// <para>
/// <b>UpdateBy(Specification, SetPropertyCalls)는</b> 이 인터페이스에 포함되지 않습니다.
/// SetPropertyCalls(EF Core)는 Adapter 계층 타입이므로 Domain 계층에서 참조할 수 없습니다(의존성 방향 위반).
/// 대신 EfCoreRepositoryBase에서 protected 메서드로 제공하며,
/// 서브클래스에서 도메인 특화 메서드로 래핑하여 사용합니다.
/// </para>
/// </remarks>
/// <typeparam name="TAggregate">Aggregate Root 타입</typeparam>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
public interface IRepository<TAggregate, TId> : IObservablePort
    where TAggregate : AggregateRoot<TId>
    where TId : struct, IEntityId<TId>
{
    // ── Write: Single ──────────────────────────────────

    /// <summary>
    /// Aggregate를 생성합니다.
    /// </summary>
    FinT<IO, TAggregate> Create(TAggregate aggregate);

    /// <summary>
    /// Aggregate를 업데이트합니다. ExecuteUpdate로 Change Tracker를 우회합니다.
    /// </summary>
    FinT<IO, TAggregate> Update(TAggregate aggregate);

    /// <summary>
    /// ID로 Aggregate를 삭제합니다. 삭제된 건수를 반환합니다.
    /// </summary>
    FinT<IO, int> Delete(TId id);

    // ── Write: Batch ───────────────────────────────────

    /// <summary>
    /// 여러 Aggregate를 일괄 생성합니다. 대량 데이터 시 청크 단위로 처리됩니다.
    /// 생성된 건수를 반환합니다.
    /// </summary>
    FinT<IO, int> CreateRange(IReadOnlyList<TAggregate> aggregates);

    /// <summary>
    /// 여러 Aggregate를 일괄 업데이트합니다. ExecuteUpdate로 SELECT를 생략합니다.
    /// 업데이트된 건수를 반환합니다.
    /// </summary>
    FinT<IO, int> UpdateRange(IReadOnlyList<TAggregate> aggregates);

    /// <summary>
    /// 여러 ID로 Aggregate를 일괄 삭제합니다. 삭제된 건수를 반환합니다.
    /// </summary>
    FinT<IO, int> DeleteRange(IReadOnlyList<TId> ids);

    // ── Read ───────────────────────────────────────────

    /// <summary>
    /// ID로 Aggregate를 조회합니다.
    /// </summary>
    FinT<IO, TAggregate> GetById(TId id);

    /// <summary>
    /// 여러 ID로 Aggregate를 일괄 조회합니다.
    /// </summary>
    FinT<IO, Seq<TAggregate>> GetByIds(IReadOnlyList<TId> ids);

    // ── Specification ──────────────────────────────────

    /// <summary>
    /// Specification 조건에 매칭되는 Aggregate가 존재하는지 확인합니다. (1 SQL)
    /// Write-side 불변식 체크 용도(예: Create 직전 중복 확인).
    /// 리포팅 용도라면 IQueryPort.Exists 사용을 고려하십시오.
    /// </summary>
    FinT<IO, bool> Exists(Specification<TAggregate> spec);

    /// <summary>
    /// Specification 조건에 매칭되는 Aggregate 건수를 반환합니다. (1 SQL)
    /// </summary>
    FinT<IO, int> Count(Specification<TAggregate> spec);

    /// <summary>
    /// Specification 조건에 매칭되는 모든 Aggregate를 조회합니다. PropertyMap 필수.
    /// Evans의 selectSatisfying 패턴 — Specification이 정의하는 도메인 규칙을
    /// Repository가 조회 조건으로 해석하여 Aggregate 컬렉션을 반환합니다.
    /// </summary>
    /// <remarks>
    /// 결과가 대량(수천 건 이상)이 예상되면 IQueryPort.Stream 사용을 고려하십시오.
    /// Aggregate 재구성 비용이 필요 없으면 IQueryPort.Search로 DTO 프로젝션을 권장합니다.
    /// </remarks>
    FinT<IO, Seq<TAggregate>> FindAllSatisfying(Specification<TAggregate> spec);

    /// <summary>
    /// Specification 조건에 매칭되는 첫 Aggregate를 조회합니다. PropertyMap 필수.
    /// 매칭이 없으면 Fin.Succ(Option.None)을 반환합니다.
    /// </summary>
    FinT<IO, Option<TAggregate>> FindFirstSatisfying(Specification<TAggregate> spec);

    /// <summary>
    /// ⚠️ Hard delete by condition. 1 SQL DELETE. 도메인 이벤트를 발행하지 않습니다.
    /// 비즈니스 삭제 시 각 Aggregate별 이벤트가 필요하면
    /// FindAllSatisfying → aggregate.SoftDelete() → UpdateRange 경로를 사용하십시오.
    /// 본 메서드는 관리·마이그레이션·테스트 픽스처 정리 용도입니다.
    /// </summary>
    FinT<IO, int> DeleteBy(Specification<TAggregate> spec);
}
