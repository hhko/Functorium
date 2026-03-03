using LanguageExt;
using LanguageExt.Common;

namespace Framework.Layers.Domains;

/// <summary>
/// LanguageExt 기반 값 객체를 위한 기본 클래스
/// 복합 값 객체의 공통 기능과 Validation 조합 헬퍼를 제공
/// </summary>
[Serializable]
public abstract class ValueObject : AbstractValueObject
{
    /// <summary>
    /// LanguageExt Validation을 사용한 팩토리 메서드 템플릿
    /// </summary>
    /// <typeparam name="TValueObject">생성할 값 객체 타입</typeparam>
    /// <typeparam name="TValue">검증할 값의 타입</typeparam>
    /// <param name="validation">LanguageExt Validation</param>
    /// <param name="factory">검증된 값으로 값 객체를 생성하는 팩토리 함수</param>
    /// <returns>Fin<TValueObject> - 성공 시 값 객체, 실패 시 Error</returns>
    public static Fin<TValueObject> CreateFromValidation<TValueObject, TValue>(
        Validation<Error, TValue> validation,
        Func<TValue, TValueObject> factory)
        where TValueObject : ValueObject
    {
        return validation
            .Map(factory)
            .ToFin();
    }
}
