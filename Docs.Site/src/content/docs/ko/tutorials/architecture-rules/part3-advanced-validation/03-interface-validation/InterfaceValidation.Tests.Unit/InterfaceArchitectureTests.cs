using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace InterfaceValidation.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(InterfaceValidation.Domains.Order).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(InterfaceValidation.Domains.Order).Namespace!;
}

[Trait("Part3-Advanced-Validation", "InterfaceValidation")]
public sealed class InterfaceArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void AllInterfaces_ShouldHave_NameStartingWithI()
    {
        ArchRuleDefinition.Interfaces()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllInterfaces(Architecture, iface => iface
                .RequireNameStartsWith("I"),
                verbose: true)
            .ThrowIfAnyFailures("Interface Naming Convention Rule");
    }

    [Fact]
    public void ConcreteRepositoryInterfaces_ShouldHave_NameEndingWithRepository()
    {
        // IOrderRepository, IProductRepository 등 구체적 Repository 인터페이스만 검증
        // 제네릭 기반 인터페이스(IRepository`1)는 ArchUnitNET에서 이름에 `1이 포함되므로 제외
        ArchRuleDefinition.Interfaces()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameEndingWith("Repository")
            .ValidateAllInterfaces(Architecture, iface => iface
                .RequireNameEndsWith("Repository"),
                verbose: true)
            .ThrowIfAnyFailures("Repository Interface Naming Rule");
    }

    [Fact]
    public void BaseRepositoryInterface_ShouldHave_GetByIdAsyncReturningTask()
    {
        // IRepository<T>에 정의된 GetByIdAsync 메서드 검증
        // 상속받은 인터페이스(IOrderRepository 등)는 직접 선언한 멤버만 가지므로
        // 기반 인터페이스를 직접 대상으로 검증
        ArchRuleDefinition.Interfaces()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameStartingWith("IRepository")
            .ValidateAllInterfaces(Architecture, iface => iface
                .RequireMethod("GetByIdAsync", m => m
                    .RequireReturnTypeContaining("Task")),
                verbose: true)
            .ThrowIfAnyFailures("Repository GetByIdAsync Rule");
    }

    [Fact]
    public void BaseRepositoryInterface_ShouldHave_SaveAsyncReturningTask()
    {
        // IRepository<T>에 정의된 SaveAsync 메서드 검증
        ArchRuleDefinition.Interfaces()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveNameStartingWith("IRepository")
            .ValidateAllInterfaces(Architecture, iface => iface
                .RequireMethod("SaveAsync", m => m
                    .RequireReturnTypeContaining("Task")),
                verbose: true)
            .ThrowIfAnyFailures("Repository SaveAsync Rule");
    }
}
