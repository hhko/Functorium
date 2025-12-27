using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using static LanguageExt.Prelude;

namespace ComparableSimpleValueObject.ValueObjects;

/// <summary>
/// 2. 비교 가능한 primitive 값 객체 - ComparableSimpleValueObject<T>
/// 사용자 ID를 나타내는 값 객체 (int 기반)
/// </summary>
public sealed class UserId : ComparableSimpleValueObject<int>
{
    private UserId(int value) 
        : base(value) 
    {
    }

    /// <summary>
    /// 사용자 ID 값에 대한 public 접근자
    /// </summary>
    public int Id => Value;

    /// <summary>
    /// 사용자 ID 값 객체 생성
    /// </summary>
    /// <param name="value">사용자 ID 값</param>
    /// <returns>Fin<UserId> - 성공 시 UserId, 실패 시 Error</returns>
    public static Fin<UserId> Create(int value) =>
        CreateFromValidation(Validate(value), val => new UserId(val));

    /// <summary>
    /// 이미 검증된 사용자 ID로 값 객체 생성
    /// </summary>
    /// <param name="validatedValue">검증된 사용자 ID</param>
    /// <returns>UserId 값 객체</returns>
    internal static UserId CreateFromValidated(int validatedValue) =>
        new UserId(validatedValue);

    /// <summary>
    /// 사용자 ID 유효성 검증
    /// </summary>
    /// <param name="value">사용자 ID 값</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, int> Validate(int value) =>
        value > 0
            ? value
            : DomainErrors.NotPositive(value);

    public static implicit operator int(UserId userId) => 
        userId.Value;

    internal static class DomainErrors
    {
        public static Error NotPositive(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(UserId)}.{nameof(NotPositive)}",
                errorCurrentValue: value,
                errorMessage: "");
    }
}
