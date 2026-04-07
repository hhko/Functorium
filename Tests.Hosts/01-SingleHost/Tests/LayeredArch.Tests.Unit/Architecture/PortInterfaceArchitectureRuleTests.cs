using ArchUnitNET.Fluent;
using Functorium.Abstractions.Observabilities;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class PortInterfaceArchitectureRuleTests
{
    // NOTE: ArchUnitNET v0.13.x의 Interfaces().That().ResideInNamespace()는
    // 하위 네임스페이스를 포함하지 않는 제한이 있으므로 HaveNameEndingWith로 필터링합니다.

    [Fact]
    public void RepositoryPort_ShouldFollow_NamingConvention()
    {
        ArchRuleDefinition.Interfaces()
            .That()
            .HaveNameEndingWith("Repository")
            .ValidateAllInterfaces(ArchitectureTestBase.Architecture, @interface => @interface
                .RequireNameStartsWith("I"),
                verbose: true)
            .ThrowIfAnyFailures("Repository Port Naming Convention Rule");
    }

    [Fact]
    public void RepositoryPort_ShouldImplement_IObservablePort()
    {
        ArchRuleDefinition.Interfaces()
            .That()
            .HaveNameEndingWith("Repository")
            .ValidateAllInterfaces(ArchitectureTestBase.Architecture, @interface => @interface
                .RequireImplements(typeof(IObservablePort)),
                verbose: true)
            .ThrowIfAnyFailures("Repository Port IObservablePort Implementation Rule");
    }

    [Fact]
    public void RepositoryPort_Methods_ShouldReturn_FinT()
    {
        ArchRuleDefinition.Interfaces()
            .That()
            .HaveNameEndingWith("Repository")
            .ValidateAllInterfaces(ArchitectureTestBase.Architecture, @interface => @interface
                .RequireAllMethods(method => method
                    .RequireReturnTypeContaining("FinT")),
                verbose: true)
            .ThrowIfAnyFailures("Repository Port Methods Return FinT Rule");
    }
}
