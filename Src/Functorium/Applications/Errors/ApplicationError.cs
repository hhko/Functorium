using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using Functorium.Abstractions.Errors;
using LanguageExt.Common;

namespace Functorium.Applications.Errors;

/// <summary>
/// 유스케이스의 애플리케이션 오류 생성을 위한 정적 팩토리 클래스.
/// 에러 코드를 자동으로 "Application.{UsecaseName}.{ErrorName}" 형식으로 생성합니다.
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Applications.Errors.ApplicationErrorType;
///
/// ApplicationError.For&lt;CreateProductCommand&gt;(new AlreadyExists(), productId, "Product already exists");
/// ApplicationError.For&lt;UpdateOrderCommand&gt;(new ValidationFailed("Quantity"), value, "Quantity must be positive");
/// // 커스텀 에러: sealed record 파생 정의
/// // public sealed record CannotDeleteShipped : ApplicationErrorType.Custom;
/// ApplicationError.For&lt;DeleteOrderCommand&gt;(new CannotDeleteShipped(), orderId, "Cannot delete shipped order");
/// </code>
/// </remarks>
public static class ApplicationError
{
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase>(
        ApplicationErrorType errorType,
        string currentValue,
        string message) =>
        LayerErrorCore.Create<TUsecase>(ErrorCodePrefixes.Application, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase, TValue>(
        ApplicationErrorType errorType,
        TValue currentValue,
        string message)
        where TValue : notnull =>
        LayerErrorCore.Create<TUsecase, TValue>(ErrorCodePrefixes.Application, errorType, currentValue, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase, T1, T2>(
        ApplicationErrorType errorType,
        T1 value1,
        T2 value2,
        string message)
        where T1 : notnull
        where T2 : notnull =>
        LayerErrorCore.Create<TUsecase, T1, T2>(ErrorCodePrefixes.Application, errorType, value1, value2, message);

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Error For<TUsecase, T1, T2, T3>(
        ApplicationErrorType errorType,
        T1 value1,
        T2 value2,
        T3 value3,
        string message)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull =>
        LayerErrorCore.Create<TUsecase, T1, T2, T3>(ErrorCodePrefixes.Application, errorType, value1, value2, value3, message);
}
