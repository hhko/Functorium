using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations.Typed;

/// <summary>
/// 값 객체 타입 정보를 체이닝 중 전달하기 위한 wrapper
/// </summary>
/// <typeparam name="TValueObject">값 객체 타입</typeparam>
/// <typeparam name="T">값 타입</typeparam>
public readonly struct TypedValidation<TValueObject, T>
{
    /// <summary>
    /// 내부 검증 결과
    /// </summary>
    public Validation<Error, T> Value { get; }

    /// <summary>
    /// TypedValidation 생성
    /// </summary>
    /// <param name="value">검증 결과</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal TypedValidation(Validation<Error, T> value) => Value = value;

    /// <summary>
    /// Validation&lt;Error, T&gt;로 암시적 변환
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Validation<Error, T>(TypedValidation<TValueObject, T> typed)
        => typed.Value;
}
