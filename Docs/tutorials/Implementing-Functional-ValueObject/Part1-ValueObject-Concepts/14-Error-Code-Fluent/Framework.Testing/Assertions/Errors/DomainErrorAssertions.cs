using Framework.Abstractions.Errors;
using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;
using Shouldly;
using static LanguageExt.Prelude;

namespace Framework.Testing.Assertions.Errors;

/// <summary>
/// 도메인 에러 검증을 위한 타입 안전 확장 메서드
/// </summary>
/// <remarks>
/// 이 클래스는 Chapter 14의 학습 목적에 맞춰 단순화된 버전입니다.
/// 향후 챕터에서 더 고급 기능이 추가될 예정입니다.
/// </remarks>
public static class DomainErrorAssertions
{
    /// <summary>
    /// Validation에서 에러들을 추출합니다.
    /// </summary>
    private static Seq<Error> GetErrors<T>(Validation<Error, T> validation) =>
        validation.Match(
            Succ: _ => Empty,
            Fail: e => e.AsIterable().ToSeq());

    /// <summary>
    /// Fin 실패 결과가 특정 DomainError인지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldBeDomainError<TDomain, T>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType)
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";

        fin.IfFail(error =>
        {
            if (error is IHasErrorCode hasErrorCode)
            {
                hasErrorCode.ErrorCode.ShouldBe(expectedErrorCode);
            }
            else
            {
                throw new ShouldAssertException($"Error should implement IHasErrorCode, but was {error.GetType().Name}");
            }
        });
    }

    /// <summary>
    /// Fin 실패 결과가 특정 DomainError인지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Fin의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="fin">검증할 Fin</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldBeDomainError<TDomain, T, TValue>(
        this Fin<T> fin,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        fin.IsFail.ShouldBeTrue("Fin should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";

        fin.IfFail(error =>
        {
            if (error is IHasErrorCode hasErrorCode)
            {
                hasErrorCode.ErrorCode.ShouldBe(expectedErrorCode);
            }
            else
            {
                throw new ShouldAssertException($"Error should implement IHasErrorCode, but was {error.GetType().Name}");
            }
        });
    }

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldHaveDomainError<TDomain, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = GetErrors(validation);

        var hasMatchingError = errors.Any(e =>
            e is IHasErrorCode hasErrorCode && hasErrorCode.ErrorCode == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors.");
    }

    /// <summary>
    /// Validation 실패 결과가 특정 DomainError를 포함하는지 검증 (현재 값 포함)
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <typeparam name="TValue">에러의 현재 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    /// <param name="expectedCurrentValue">기대하는 현재 값</param>
    public static void ShouldHaveDomainError<TDomain, T, TValue>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType,
        TValue expectedCurrentValue)
        where TValue : notnull
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = GetErrors(validation);

        var hasMatchingError = errors.Any(e =>
            e is IHasErrorCode hasErrorCode && hasErrorCode.ErrorCode == expectedErrorCode);

        hasMatchingError.ShouldBeTrue(
            $"Expected error code '{expectedErrorCode}' not found in validation errors.");
    }

    /// <summary>
    /// Validation 실패 결과가 정확히 해당 DomainError 하나만 포함하는지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorType">기대하는 에러 타입</param>
    public static void ShouldHaveOnlyDomainError<TDomain, T>(
        this Validation<Error, T> validation,
        DomainErrorType expectedErrorType)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCode = $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{expectedErrorType.ErrorName}";
        var errors = GetErrors(validation);

        errors.Count.ShouldBe(1, $"Expected exactly 1 error but found {errors.Count}");

        var singleError = errors.First();
        if (singleError is IHasErrorCode hasErrorCode)
        {
            hasErrorCode.ErrorCode.ShouldBe(expectedErrorCode);
        }
        else
        {
            throw new ShouldAssertException($"Error should implement IHasErrorCode, but was {singleError.GetType().Name}");
        }
    }

    /// <summary>
    /// Validation 실패 결과에 여러 DomainError가 모두 포함되어 있는지 검증
    /// </summary>
    /// <typeparam name="TDomain">도메인 타입 (Value Object, Entity, Aggregate 등)</typeparam>
    /// <typeparam name="T">Validation의 성공 값 타입</typeparam>
    /// <param name="validation">검증할 Validation</param>
    /// <param name="expectedErrorTypes">기대하는 에러 타입들</param>
    public static void ShouldHaveDomainErrors<TDomain, T>(
        this Validation<Error, T> validation,
        params DomainErrorType[] expectedErrorTypes)
    {
        validation.IsFail.ShouldBeTrue("Validation should have failed");

        var expectedErrorCodes = expectedErrorTypes
            .Select(et => $"{ErrorType.DomainErrorsPrefix}.{typeof(TDomain).Name}.{et.ErrorName}")
            .ToList();

        var errors = GetErrors(validation);

        var actualErrorCodes = errors
            .OfType<IHasErrorCode>()
            .Select(e => e.ErrorCode)
            .ToList();

        foreach (var expectedCode in expectedErrorCodes)
        {
            actualErrorCodes.ShouldContain(expectedCode,
                $"Expected error code '{expectedCode}' not found. " +
                $"Actual error codes: [{string.Join(", ", actualErrorCodes)}]");
        }
    }
}
