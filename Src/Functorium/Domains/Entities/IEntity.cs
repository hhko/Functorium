namespace Functorium.Domains.Entities;

/// <summary>
/// Entity 명명 규칙 상수를 정의하는 비제네릭 인터페이스.
/// </summary>
public interface IEntity
{
    /// <summary>
    /// 새 Entity를 생성하는 팩토리 메서드 이름.
    /// </summary>
    const string CreateMethodName = "Create";

    /// <summary>
    /// 이미 검증된 데이터로 Entity를 생성하는 메서드 이름 (Repository/ORM 복원용).
    /// </summary>
    const string CreateFromValidatedMethodName = "CreateFromValidated";
}

/// <summary>
/// Entity의 기본 인터페이스.
/// ID 기반 동등성을 정의합니다.
/// </summary>
/// <typeparam name="TId">EntityId 구현 타입</typeparam>
public interface IEntity<TId> : IEntity
    where TId : struct, IEntityId<TId>
{
    /// <summary>
    /// Entity의 고유 식별자.
    /// </summary>
    TId Id { get; }
}
