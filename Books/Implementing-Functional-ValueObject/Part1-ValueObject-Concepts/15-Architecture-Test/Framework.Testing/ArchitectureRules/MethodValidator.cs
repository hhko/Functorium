using ArchUnitNET.Domain;

namespace Framework.Test.ArchitectureRules;

/// <summary>
/// 메서드에 대한 아키텍처 규칙 검증을 수행하는 클래스입니다.
/// 
/// 이 클래스는 단일 메서드에 대한 다양한 아키텍처 규칙을 검증하는 기능을 제공합니다.
/// ClassValidator와 연동하여 메서드 수준의 세부적인 검증을 수행하며,
/// 메서드 체이닝 패턴을 통해 여러 검증 규칙을 연속적으로 적용할 수 있습니다.
/// 
/// 주요 검증 기능:
/// - 메서드 가시성 검증 (public, private, protected, internal)
/// - static 메서드 여부 검증
/// - 반환 타입 검증 (정확한 타입 또는 제네릭 타입)
/// - 선언 클래스와 동일한 반환 타입 검증
/// 
/// 사용 예시:
/// <code>
/// @class.RequireMethod("Create", method => method
///     .RequireVisibility(Visibility.Public)
///     .RequireStatic()
///     .RequireReturnType(typeof(Fin&lt;&gt;)));
/// </code>
/// </summary>
public sealed class MethodValidator
{
    private readonly MethodMember _targetMethod;
    private readonly List<string> _failures;

    /// <summary>
    /// MethodValidator 인스턴스를 생성합니다.
    /// 
    /// 부모 ClassValidator와 동일한 failures 리스트를 공유하여
    /// 검증 실패 시 부모 클래스의 검증 결과에 자동으로 포함됩니다.
    /// </summary>
    /// <param name="targetMethod">검증할 대상 메서드</param>
    /// <param name="parentValidator">부모 ClassValidator 인스턴스</param>
    public MethodValidator(MethodMember targetMethod, ClassValidator parentValidator)
    {
        _targetMethod = targetMethod;
        _failures = parentValidator._failures; // 부모와 같은 failures 리스트 공유
    }

    /// <summary>
    /// 메서드의 가시성을 검증합니다.
    /// 
    /// 메서드가 지정된 가시성(public, private, protected, internal)을
    /// 가지고 있는지 확인합니다.
    /// 
    /// 사용 예시:
    /// <code>
    /// method.RequireVisibility(Visibility.Public)  // public 메서드여야 함
    /// method.RequireVisibility(Visibility.Private) // private 메서드여야 함
    /// </code>
    /// </summary>
    /// <param name="visibility">요구되는 가시성</param>
    /// <returns>MethodValidator 인스턴스 (메서드 체이닝용)</returns>
    public MethodValidator RequireVisibility(Visibility visibility)
    {
        if (_targetMethod.Visibility != visibility)
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be {visibility.ToString().ToLower()}.");
        }
        return this;
    }

    /// <summary>
    /// 메서드가 static 메서드인지 검증합니다.
    /// 
    /// 팩토리 메서드나 유틸리티 메서드의 경우 static이어야 하는
    /// 아키텍처 규칙을 검증할 때 사용됩니다.
    /// 
    /// 사용 예시:
    /// <code>
    /// method.RequireStatic()  // static 메서드여야 함
    /// </code>
    /// </summary>
    /// <returns>MethodValidator 인스턴스 (메서드 체이닝용)</returns>
    public MethodValidator RequireStatic()
    {
        if (_targetMethod.IsStatic != true)
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be static.");
        }
        return this;
    }

    /// <summary>
    /// 메서드의 반환 타입을 검증합니다.
    /// 
    /// 메서드가 지정된 타입을 반환하는지 확인합니다.
    /// 제네릭 타입의 경우 제네릭 타입 정의와 호환되는지 검사합니다.
    /// 
    /// 지원하는 타입 매칭:
    /// - 정확한 타입 매칭 (예: string, int)
    /// - 제네릭 타입 매칭 (예: Fin&lt;T&gt;, Validation&lt;Error, T&gt;)
    /// - object 타입 (모든 타입과 호환)
    /// 
    /// 사용 예시:
    /// <code>
    /// method.RequireReturnType(typeof(string))        // string 반환
    /// method.RequireReturnType(typeof(Fin&lt;&gt;))        // Fin&lt;T&gt; 반환
    /// method.RequireReturnType(typeof(Validation&lt;,&gt;)) // Validation&lt;Error, T&gt; 반환
    /// </code>
    /// </summary>
    /// <param name="returnType">요구되는 반환 타입</param>
    /// <returns>MethodValidator 인스턴스 (메서드 체이닝용)</returns>
    public MethodValidator RequireReturnType(Type returnType)
    {
        if (!IsReturnTypeCompatible(_targetMethod.ReturnType, returnType))
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return '{returnType.Name}'.");
        }
        return this;
    }

    /// <summary>
    /// 메서드가 선언 클래스와 동일한 타입을 반환하는지 검증합니다.
    /// 
    /// 주로 팩토리 메서드나 생성자 대체 메서드에서 사용되며,
    /// 메서드가 자신이 속한 클래스의 인스턴스를 반환하는지 확인합니다.
    /// 
    /// 사용 예시:
    /// <code>
    /// // Email 클래스의 Create 메서드가 Email 타입을 반환해야 함
    /// method.RequireReturnTypeOfDeclaringClass()
    /// </code>
    /// </summary>
    /// <returns>MethodValidator 인스턴스 (메서드 체이닝용)</returns>
    public MethodValidator RequireReturnTypeOfDeclaringClass()
    {
        var declaringClassName = _targetMethod.DeclaringType.Name;
        if (_targetMethod.ReturnType.Name != declaringClassName)
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return '{declaringClassName}'.");
        }
        return this;
    }

    /// <summary>
    /// 실제 반환 타입이 예상 반환 타입과 호환되는지 확인합니다.
    /// 
    /// 이 메서드는 다양한 타입 매칭 시나리오를 지원합니다:
    /// 
    /// 1. **정확한 타입 매칭**
    ///    - FullName이 정확히 일치하는 경우
    /// 
    /// 2. **제네릭 타입 매칭**
    ///    - 제네릭 타입 정의와 호환되는지 확인
    ///    - 예: Fin&lt;T&gt;는 Fin&lt;string&gt;, Fin&lt;int&gt; 등과 호환
    ///    - 예: Validation&lt;Error, T&gt;는 Validation&lt;Error, string&gt; 등과 호환
    /// 
    /// 3. **object 타입 호환성**
    ///    - object 타입은 모든 타입과 호환
    /// 
    /// 제네릭 타입 매칭 시 `1, `2 등의 제네릭 매개변수 표시자를 제거하여
    /// 타입 정의 이름만으로 비교합니다.
    /// </summary>
    /// <param name="actualReturnType">실제 반환 타입</param>
    /// <param name="expectedReturnType">예상 반환 타입</param>
    /// <returns>호환되면 true, 그렇지 않으면 false</returns>
    private static bool IsReturnTypeCompatible(IType actualReturnType, Type expectedReturnType)
    {
        // 정확한 타입 매칭
        if (actualReturnType.FullName == expectedReturnType.FullName)
        {
            return true;
        }

        // Generic 타입 매칭 (예: Fin<T>, Validation<Error, T> 등)
        if (expectedReturnType.IsGenericTypeDefinition)
        {
            return actualReturnType.FullName?.StartsWith(expectedReturnType.FullName?
                .Replace("`1", "")
                .Replace("`2", "")
                .Replace("`3", "")
                .Replace("`4", "") ?? "") == true;
        }

        // object 타입은 모든 타입과 호환
        if (expectedReturnType == typeof(object))
        {
            return true;
        }

        return false;
    }
}
