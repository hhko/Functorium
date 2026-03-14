namespace Functorium.Domains.ValueObjects;

/// <summary>
/// DU switch 식의 도달 불가능한 기본 케이스에서 발생하는 예외.
/// Source Generator가 생성하는 Match/Switch의 기본 분기에 사용합니다.
/// </summary>
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
