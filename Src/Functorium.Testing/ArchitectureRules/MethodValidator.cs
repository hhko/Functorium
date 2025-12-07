using ArchUnitNET.Domain;

namespace Functorium.Testing.ArchitectureRules;

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

    public MethodValidator(MethodMember targetMethod, ClassValidator parentValidator)
    {
        _targetMethod = targetMethod;
        _failures = parentValidator._failures; // 부모와 같은 failures 리스트 공유
    }

    public MethodValidator RequireVisibility(Visibility visibility)
    {
        if (_targetMethod.Visibility != visibility)
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be {visibility.ToString().ToLower()}.");
        }
        return this;
    }

    public MethodValidator RequireStatic()
    {
        if (_targetMethod.IsStatic != true)
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be static.");
        }
        return this;
    }

    public MethodValidator RequireReturnType(Type returnType)
    {
        if (!IsReturnTypeCompatible(_targetMethod.ReturnType, returnType))
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return '{returnType.Name}'.");
        }
        return this;
    }

    public MethodValidator RequireReturnTypeOfDeclaringClass()
    {
        var declaringClassName = _targetMethod.DeclaringType.Name;
        if (_targetMethod.ReturnType.Name != declaringClassName)
        {
            _failures.Add($"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return '{declaringClassName}'.");
        }
        return this;
    }

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
