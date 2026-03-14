using Functorium.Domains.Errors;
using LanguageExt;

namespace Functorium.Domains.ValueObjects;

/// <summary>
/// 상태 전이를 지원하는 Discriminated Union 값 객체.
/// CRTP(Curiously Recurring Template Pattern)으로 DomainError에 정확한 타입 정보를 전달합니다.
/// </summary>
[Serializable]
public abstract record UnionValueObject<TSelf> : UnionValueObject
    where TSelf : UnionValueObject<TSelf>
{
    /// <summary>
    /// 상태 전이 헬퍼. this가 TSource이면 전이 함수 적용, 아니면 InvalidTransition 에러 반환.
    /// </summary>
    protected Fin<TTarget> TransitionFrom<TSource, TTarget>(
        Func<TSource, TTarget> transition,
        string? message = null)
        where TTarget : notnull
    {
        if (this is TSource source)
            return transition(source);

        return Fin.Fail<TTarget>(
            DomainError.For<TSelf>(
                new DomainErrorType.InvalidTransition(
                    FromState: GetType().Name,
                    ToState: typeof(TTarget).Name),
                ToString()!,
                message ?? $"Invalid transition from {GetType().Name} to {typeof(TTarget).Name}"));
    }
}
