namespace Functorium.Domains.ValueObjects.Unions;

/// <summary>
/// Discriminated Union 값 객체에 Match/Switch 메서드를 자동 생성합니다.
/// abstract partial record에 적용하면 내부 sealed record 케이스를 분석하여 생성합니다.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class UnionTypeAttribute : Attribute;
