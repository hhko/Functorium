namespace Functorium.Domains.Entities;

/// <summary>
/// Entity ID의 기본 인터페이스.
/// Ulid 기반으로 시간 순서 정렬이 가능합니다.
/// </summary>
/// <typeparam name="T">EntityId 구현 타입</typeparam>
public interface IEntityId<T> : IEquatable<T>, IComparable<T>
    where T : struct, IEntityId<T>
{
    /// <summary>
    /// Ulid 값.
    /// </summary>
    Ulid Value { get; }

    /// <summary>
    /// 새로운 EntityId를 생성합니다.
    /// </summary>
    static abstract T New();

    /// <summary>
    /// Ulid로부터 EntityId를 생성합니다.
    /// </summary>
    static abstract T Create(Ulid id);

    /// <summary>
    /// 문자열로부터 EntityId를 생성합니다.
    /// </summary>
    /// <exception cref="FormatException">유효하지 않은 Ulid 형식인 경우</exception>
    static abstract T Create(string id);
}
