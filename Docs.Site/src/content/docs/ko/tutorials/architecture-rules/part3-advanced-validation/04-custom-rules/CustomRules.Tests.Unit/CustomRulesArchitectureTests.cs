using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace CustomRules.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(CustomRules.Domains.Invoice).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(CustomRules.Domains.Invoice).Namespace!;
}

[Trait("Part3-Advanced-Validation", "CustomRules")]
public sealed class CustomRulesArchitectureTests : ArchitectureTestBase
{
    // --- DelegateArchRule: 람다 기반 커스텀 규칙 ---

    private static readonly DelegateArchRule<Class> s_factoryMethodRule = new(
        "All domain classes must have a static Create factory method",
        (target, _) =>
        {
            var hasCreate = target.Members
                .OfType<MethodMember>()
                .Any(m => m.Name.StartsWith("Create(") && m.IsStatic == true);
            return hasCreate
                ? []
                : [new RuleViolation(target.FullName, "FactoryMethodRequired",
                    $"Class '{target.Name}' must have a static Create method.")];
        });

    private static readonly DelegateArchRule<Class> s_noServiceSuffixRule = new(
        "Domain classes must not have infrastructure suffixes",
        (target, _) =>
        {
            var forbiddenSuffixes = new[] { "Service", "Repository", "Controller", "Handler" };
            var violation = forbiddenSuffixes
                .Where(suffix => target.Name.EndsWith(suffix))
                .Select(suffix => new RuleViolation(target.FullName, "NoInfrastructureSuffix",
                    $"Domain class '{target.Name}' must not end with '{suffix}'."))
                .FirstOrDefault();
            return violation != null ? [violation] : [];
        });

    // --- CompositeArchRule: 여러 규칙을 AND 합성 ---

    private static readonly CompositeArchRule<Class> s_domainClassRule = new(
        s_factoryMethodRule,
        s_noServiceSuffixRule);

    [Fact]
    public void DomainClasses_ShouldHave_FactoryMethod()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .Apply(s_factoryMethodRule),
                verbose: true)
            .ThrowIfAnyFailures("Factory Method Rule");
    }

    [Fact]
    public void DomainClasses_ShouldNotHave_InfrastructureSuffix()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .Apply(s_noServiceSuffixRule),
                verbose: true)
            .ThrowIfAnyFailures("No Infrastructure Suffix Rule");
    }

    [Fact]
    public void DomainClasses_ShouldSatisfy_CompositeRule()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .Apply(s_domainClassRule),
                verbose: true)
            .ThrowIfAnyFailures("Domain Composite Rule");
    }

    [Fact]
    public void DomainClasses_ShouldSatisfy_CompositeRuleWithBuiltIn()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed()
                .RequireImmutable()
                .Apply(s_domainClassRule),
                verbose: true)
            .ThrowIfAnyFailures("Domain Full Composite Rule");
    }
}
