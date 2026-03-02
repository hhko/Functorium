using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using ArchUnitNET.Domain;

namespace Functorium.Testing.Assertions.ArchitectureRules;

/// <summary>
/// 메서드에 대한 아키텍처 규칙 검증을 수행하는 클래스입니다.
/// </summary>
public sealed class MethodValidator
{
    private static readonly ConcurrentDictionary<(string TypeFullName, string MethodName), System.Reflection.MethodInfo?> s_reflectionCache = new();

    private readonly MethodMember _targetMethod;
    private readonly List<RuleViolation> _violations;

    public MethodValidator(MethodMember targetMethod, List<RuleViolation> violations)
    {
        _targetMethod = targetMethod;
        _violations = violations;
    }

    public MethodValidator RequireVisibility(Visibility visibility)
    {
        if (_targetMethod.Visibility != visibility)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be {visibility.ToString().ToLower()}.");
        }
        return this;
    }

    public MethodValidator RequireStatic()
    {
        if (_targetMethod.IsStatic != true)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be static.");
        }
        return this;
    }

    public MethodValidator RequireExtensionMethod()
    {
        if (!_targetMethod.Attributes.Any(a =>
            a.FullName?.Contains("ExtensionAttribute") == true))
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be an extension method.");
        }
        return this;
    }

    public MethodValidator RequireReturnType(Type returnType)
    {
        if (!IsReturnTypeCompatible(_targetMethod.ReturnType, returnType))
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return '{returnType.Name}'.");
        }
        return this;
    }

    public MethodValidator RequireReturnTypeOfDeclaringClass()
    {
        var declaringClassName = _targetMethod.DeclaringType.Name;
        if (_targetMethod.ReturnType.Name != declaringClassName)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return '{declaringClassName}'.");
        }
        return this;
    }

    public MethodValidator RequireReturnTypeOfDeclaringTopLevelClass()
    {
        var declaringFullName = _targetMethod.DeclaringType.FullName;
        var topLevelClassName = declaringFullName.Contains('+')
            ? declaringFullName[..declaringFullName.IndexOf('+')].Split('.')[^1]
            : _targetMethod.DeclaringType.Name;

        if (_targetMethod.ReturnType.Name != topLevelClassName)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must return top-level class '{topLevelClassName}'.");
        }
        return this;
    }

    public MethodValidator RequireVirtual()
    {
        if (_targetMethod.MethodForm != MethodForm.Normal)
            return this;

        if (_targetMethod.Name.StartsWith("op_") || _targetMethod.Name.StartsWith("<"))
            return this;

        var reflectionMethod = ResolveReflectionMethod();
        if (reflectionMethod != null && (!reflectionMethod.IsVirtual || reflectionMethod.IsFinal))
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must be virtual.");
        }
        return this;
    }

    public MethodValidator RequireNotStatic()
    {
        if (_targetMethod.IsStatic == true)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must not be static.");
        }
        return this;
    }

    public MethodValidator RequireNotVirtual()
    {
        if (_targetMethod.MethodForm != MethodForm.Normal)
            return this;

        if (_targetMethod.Name.StartsWith("op_") || _targetMethod.Name.StartsWith("<"))
            return this;

        var reflectionMethod = ResolveReflectionMethod();
        if (reflectionMethod != null && reflectionMethod.IsVirtual && !reflectionMethod.IsFinal)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must not be virtual.");
        }
        return this;
    }

    public MethodValidator RequireParameterCount(int expectedCount)
    {
        var reflectionMethod = ResolveReflectionMethod();
        if (reflectionMethod != null)
        {
            var actualCount = reflectionMethod.GetParameters().Length;
            if (actualCount != expectedCount)
            {
                AddViolation(
                    $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must have {expectedCount} parameter(s), but has {actualCount}.");
            }
        }
        return this;
    }

    public MethodValidator RequireParameterCountAtLeast(int minimumCount)
    {
        var reflectionMethod = ResolveReflectionMethod();
        if (reflectionMethod != null)
        {
            var actualCount = reflectionMethod.GetParameters().Length;
            if (actualCount < minimumCount)
            {
                AddViolation(
                    $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must have at least {minimumCount} parameter(s), but has {actualCount}.");
            }
        }
        return this;
    }

    public MethodValidator RequireFirstParameterTypeContaining(string typeNameFragment)
    {
        var reflectionMethod = ResolveReflectionMethod();
        if (reflectionMethod != null)
        {
            var parameters = reflectionMethod.GetParameters();
            if (parameters.Length == 0)
            {
                AddViolation(
                    $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must have at least one parameter to check first parameter type.");
            }
            else if (!parameters[0].ParameterType.FullName?.Contains(typeNameFragment) == true
                     && !parameters[0].ParameterType.Name.Contains(typeNameFragment))
            {
                AddViolation(
                    $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' first parameter type must contain '{typeNameFragment}', but is '{parameters[0].ParameterType.Name}'.");
            }
        }
        return this;
    }

    public MethodValidator RequireAnyParameterTypeContaining(string typeNameFragment)
    {
        var reflectionMethod = ResolveReflectionMethod();
        if (reflectionMethod != null)
        {
            var parameters = reflectionMethod.GetParameters();
            if (!parameters.Any(p =>
                p.ParameterType.FullName?.Contains(typeNameFragment) == true
                || p.ParameterType.Name.Contains(typeNameFragment)))
            {
                AddViolation(
                    $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must have a parameter with type containing '{typeNameFragment}'.");
            }
        }
        return this;
    }

    public MethodValidator RequireReturnTypeContaining(string typeNameFragment)
    {
        if (_targetMethod.ReturnType.FullName?.Contains(typeNameFragment) != true)
        {
            AddViolation(
                $"Method '{_targetMethod.Name}' in class '{_targetMethod.DeclaringType.Name}' must have return type containing '{typeNameFragment}'.");
        }
        return this;
    }

    private System.Reflection.MethodInfo? ResolveReflectionMethod()
    {
        var declaringTypeFullName = _targetMethod.DeclaringType.FullName;
        var methodName = _targetMethod.Name.Contains('(')
            ? _targetMethod.Name[.._targetMethod.Name.IndexOf('(')]
            : _targetMethod.Name;

        return s_reflectionCache.GetOrAdd((declaringTypeFullName, methodName), key =>
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .Select(a => a.GetType(key.TypeFullName))
                .FirstOrDefault(t => t != null);

            if (type == null)
                return null;

            return type.GetMethods(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.DeclaredOnly)
                .FirstOrDefault(m => m.Name == methodName);
        });
    }

    private static bool IsReturnTypeCompatible(IType actualReturnType, Type expectedReturnType)
    {
        if (actualReturnType.FullName == expectedReturnType.FullName)
        {
            return true;
        }

        if (expectedReturnType.IsGenericTypeDefinition)
        {
            var prefix = Regex.Replace(expectedReturnType.FullName ?? "", @"`\d+", "");
            return actualReturnType.FullName?.StartsWith(prefix) == true;
        }

        if (expectedReturnType == typeof(object))
        {
            return true;
        }

        return false;
    }

    private void AddViolation(string description, [CallerMemberName] string ruleName = "")
    {
        _violations.Add(new RuleViolation(_targetMethod.DeclaringType.FullName, ruleName, description));
    }
}
