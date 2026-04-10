namespace UnionValueObject.ValueObjects;

/// <summary>
/// DU switch 식의 도달 불가능한 기본 케이스에서 발생하는 예외.
/// </summary>
public sealed class UnreachableCaseException(object value)
    : InvalidOperationException($"Unreachable case: {value.GetType().FullName}");
