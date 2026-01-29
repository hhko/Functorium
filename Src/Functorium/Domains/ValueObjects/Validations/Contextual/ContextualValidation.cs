using System.Runtime.CompilerServices;
using LanguageExt;
using LanguageExt.Common;

namespace Functorium.Domains.ValueObjects.Validations.Contextual;

/// <summary>
/// 검증 컨텍스트 이름을 체이닝 중 전달하기 위한 wrapper
/// </summary>
/// <typeparam name="T">값 타입</typeparam>
/// <remarks>
/// <para>
/// Named Context 검증 패턴에서 컨텍스트 이름을 체이닝 메서드에 전파합니다.
/// <see cref="TypedValidation{TValueObject, T}"/>의 컨텍스트 기반 버전입니다.
/// </para>
/// </remarks>
public readonly struct ContextualValidation<T>
{
    /// <summary>
    /// 내부 검증 결과
    /// </summary>
    public Validation<Error, T> Value { get; }

    /// <summary>
    /// 검증 컨텍스트 이름
    /// </summary>
    public string ContextName { get; }

    /// <summary>
    /// ContextualValidation 생성
    /// </summary>
    /// <param name="value">검증 결과</param>
    /// <param name="contextName">컨텍스트 이름</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ContextualValidation(Validation<Error, T> value, string contextName)
    {
        Value = value;
        ContextName = contextName;
    }

    /// <summary>
    /// Validation&lt;Error, T&gt;로 암시적 변환
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Validation<Error, T>(ContextualValidation<T> contextual)
        => contextual.Value;
}
