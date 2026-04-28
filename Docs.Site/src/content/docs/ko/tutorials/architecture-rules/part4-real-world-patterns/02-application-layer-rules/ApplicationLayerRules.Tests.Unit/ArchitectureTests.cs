using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace ApplicationLayerRules.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(ApplicationLayerRules.Applications.CreateOrder).Assembly)
            .Build();

    protected static readonly string ApplicationNamespace =
        typeof(ApplicationLayerRules.Applications.CreateOrder).Namespace!;

    protected static readonly string DtoNamespace =
        typeof(ApplicationLayerRules.Applications.Dtos.OrderDto).Namespace!;
}

[Trait("Part4-Real-World-Patterns", "ApplicationLayerRules")]
public sealed class CommandArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void CreateOrder_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .HaveName("CreateOrder")
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Command Public Sealed Rule");
    }

    [Fact]
    public void CreateOrder_ShouldHave_NestedRequestResponseUsecase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .HaveName("CreateOrder")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClass("Request", nested => nested
                    .RequireSealed()
                    .RequireRecord())
                .RequireNestedClass("Response", nested => nested
                    .RequireSealed()
                    .RequireRecord())
                .RequireNestedClass("Usecase", nested => nested
                    .RequireSealed()
                    .RequireImplementsGenericInterface("ICommandUsecase")),
                verbose: true)
            .ThrowIfAnyFailures("Command Structure Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "ApplicationLayerRules")]
public sealed class QueryArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void GetOrderById_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .HaveName("GetOrderById")
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Query Public Sealed Rule");
    }

    [Fact]
    public void GetOrderById_ShouldHave_NestedRequestResponseUsecase()
    {
        ArchRuleDefinition.Classes()
            .That()
            .HaveName("GetOrderById")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClass("Request", nested => nested
                    .RequireSealed()
                    .RequireRecord())
                .RequireNestedClass("Response", nested => nested
                    .RequireSealed()
                    .RequireRecord())
                .RequireNestedClass("Usecase", nested => nested
                    .RequireSealed()
                    .RequireImplementsGenericInterface("IQueryUsecase")),
                verbose: true)
            .ThrowIfAnyFailures("Query Structure Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "ApplicationLayerRules")]
public sealed class UsecaseArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Usecases_ShouldBe_Sealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And()
            .HaveNameEndingWith("Usecase")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("Usecase Sealed Rule");
    }

    [Fact]
    public void Usecases_ShouldImplement_CommandOrQueryInterface()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And()
            .HaveNameEndingWith("Usecase")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImplementsGenericInterface("Usecase"),
                verbose: true)
            .ThrowIfAnyFailures("Usecase Interface Rule");
    }
}

[Trait("Part4-Real-World-Patterns", "ApplicationLayerRules")]
public sealed class DtoArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void Dtos_ShouldBe_PublicAndSealed()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DtoNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequirePublic()
                .RequireSealed(),
                verbose: true)
            .ThrowIfAnyFailures("DTO Public Sealed Rule");
    }

    [Fact]
    public void Dtos_ShouldHave_NoPublicSetters()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DtoNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNoPublicSetters(),
                verbose: true)
            .ThrowIfAnyFailures("DTO No Public Setters Rule");
    }
}
