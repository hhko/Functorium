using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Functorium.Domains.Entities;
using Functorium.Domains.ValueObjects;
using Functorium.Testing.Assertions.ArchitectureRules;
using Functorium.Testing.Assertions.ArchitectureRules.Rules;

namespace LayeredArch.Tests.Unit.Architecture;

/// <summary>
/// DelegateArchRule과 CompositeArchRule을 활용한 커스텀 규칙 합성 테스트입니다.
/// </summary>
public sealed class RuleCompositionTests : ArchitectureTestBase
{
    // --- DelegateArchRule: 람다 기반 커스텀 규칙 ---

    /// <summary>
    /// 도메인 클래스에 인프라 접미사(Dto, ViewModel, Handler, Controller)를 금지합니다.
    /// </summary>
    private static readonly DelegateArchRule<Class> s_domainNamingRule = new((target, _) =>
    {
        string[] forbiddenSuffixes = ["Dto", "ViewModel", "Handler", "Controller"];
        foreach (var suffix in forbiddenSuffixes)
        {
            if (target.Name.EndsWith(suffix))
                return [new RuleViolation(target.FullName, "DomainNamingConvention",
                    $"Domain class '{target.Name}' must not have '{suffix}' suffix.")];
        }
        return [];
    });

    /// <summary>
    /// 도메인 클래스가 인프라 네임스페이스(System.IO, System.Net, EntityFrameworkCore)에 의존하지 않도록 합니다.
    /// </summary>
    private static readonly DelegateArchRule<Class> s_noInfrastructureDependencyRule = new((target, _) =>
    {
        string[] forbiddenNamespaces = ["System.IO", "System.Net", "Microsoft.EntityFrameworkCore"];
        var forbidden = target.Dependencies
            .Where(d => forbiddenNamespaces.Any(ns => d.Target.FullName?.StartsWith(ns) == true))
            .Select(d => d.Target.FullName)
            .Distinct()
            .ToList();

        if (forbidden.Count == 0)
            return [];

        return [new RuleViolation(target.FullName, "NoInfrastructureDependency",
            $"Domain class '{target.Name}' must not depend on infrastructure types: {string.Join(", ", forbidden)}")];
    });

    // --- CompositeArchRule: 여러 규칙을 AND 합성 ---

    /// <summary>
    /// ValueObject 핵심 규칙: 불변성 + 네이밍 규칙 + 인프라 의존 금지를 AND로 합성합니다.
    /// </summary>
    private static readonly CompositeArchRule<Class> s_valueObjectCoreRule = new(
        new ImmutabilityRule(),
        s_domainNamingRule,
        s_noInfrastructureDependencyRule);

    // --- Tests ---

    [Fact]
    public void Entity_ShouldSatisfy_DomainNamingConvention()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .Apply(s_domainNamingRule),
                verbose: true)
            .ThrowIfAnyFailures("Entity Domain Naming Convention Rule");
    }

    [Fact]
    public void Entity_ShouldNotDependOn_InfrastructureNamespaces()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().AreAssignableTo(typeof(Entity<>))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .Apply(s_noInfrastructureDependencyRule),
                verbose: true)
            .ThrowIfAnyFailures("Entity No Infrastructure Dependency Rule");
    }

    [Fact]
    public void ValueObject_ShouldSatisfy_CompositeRule()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And().ImplementInterface(typeof(IValueObject))
            .And().AreNotAbstract()
            .ValidateAllClasses(Architecture, @class => @class
                .Apply(s_valueObjectCoreRule),
                verbose: true)
            .ThrowIfAnyFailures("ValueObject Composite Core Rule");
    }
}
