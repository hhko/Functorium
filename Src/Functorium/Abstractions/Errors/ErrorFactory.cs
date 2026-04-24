using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Functorium.Abstractions.Errors;

/// <summary>
/// Expected/Exceptional 에러 인스턴스를 생성하는 내부 팩토리.
/// </summary>
/// <remarks>
/// <para>
/// 레이어 팩토리(<see cref="Functorium.Domains.Errors.DomainError"/>·
/// <c>ApplicationError</c>·<c>AdapterError</c>)가 내부적으로 <see cref="LayerErrorCore"/>를
/// 거쳐 이 팩토리를 호출합니다. 외부 코드가 직접 참조할 일은 없으므로 internal로
/// 노출합니다 (InternalsVisibleTo 범위 안에서는 접근 가능).
/// </para>
/// <para>
/// <c>CreateExpected</c>는 비즈니스 규칙 위반 같은 예상된 실패를,
/// <c>CreateExceptional</c>은 예외(Exception)를 에러 코드로 래핑합니다. 대칭
/// 명명을 통해 호출자의 의도를 시그니처에서 즉시 파악할 수 있게 합니다.
/// </para>
/// </remarks>
internal static class ErrorFactory
{
    // ErrorCodeExpected
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateExpected(string errorCode,
                                       string errorCurrentValue,
                                       string errorMessage) =>
        new ErrorCodeExpected(
            errorCode,
            errorCurrentValue,
            errorMessage);

    // ErrorCodeExpected<T>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateExpected<T>(string errorCode,
                                          T errorCurrentValue,
                                          string errorMessage) where T : notnull =>
        new ErrorCodeExpected<T>(
            errorCode,
            errorCurrentValue,
            errorMessage);

    // ErrorCodeExpected<T1, T2>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateExpected<T1, T2>(string errorCode,
                                               T1 errorCurrentValue1,
                                               T2 errorCurrentValue2,
                                               string errorMessage) where T1 : notnull where T2 : notnull =>
        new ErrorCodeExpected<T1, T2>(
            errorCode,
            errorCurrentValue1,
            errorCurrentValue2,
            errorMessage);

    // ErrorCodeExpected<T1, T2, T3>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateExpected<T1, T2, T3>(string errorCode,
                                                   T1 errorCurrentValue1,
                                                   T2 errorCurrentValue2,
                                                   T3 errorCurrentValue3,
                                                   string errorMessage) where T1 : notnull where T2 : notnull where T3 : notnull =>
        new ErrorCodeExpected<T1, T2, T3>(
            errorCode,
            errorCurrentValue1,
            errorCurrentValue2,
            errorCurrentValue3,
            errorMessage);

    // ErrorCodeExceptional
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error CreateExceptional(string errorCode,
                                          Exception exception) =>
        new ErrorCodeExceptional(
            errorCode,
            exception);
}
