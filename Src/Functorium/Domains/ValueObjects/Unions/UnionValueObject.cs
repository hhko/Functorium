namespace Functorium.Domains.ValueObjects;

/// <summary>
/// Discriminated Union 값 객체의 기본 abstract record.
/// 순수 데이터 유니온(상태 전이 없음)에 사용합니다.
/// </summary>
[Serializable]
public abstract record UnionValueObject : IUnionValueObject;
