using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class UsecaseArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void Command_ShouldHave_ValidatorNestedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Command")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClassIfExists("Validator", nested => nested
                    .RequireSealed()
                    .RequireImplementsGenericInterface("AbstractValidator")),
                verbose: true)
            .ThrowIfAnyFailures("Command Validator Rule");
    }

    [Fact]
    public void Command_ShouldHave_UsecaseNestedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Command")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClass("Usecase", nested => nested
                    .RequireSealed()
                    .RequireImplementsGenericInterface("ICommandUsecase")),
                verbose: true)
            .ThrowIfAnyFailures("Command Usecase Rule");
    }

    [Fact]
    public void Query_ShouldHave_ValidatorNestedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Query")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClassIfExists("Validator", nested => nested
                    .RequireSealed()
                    .RequireImplementsGenericInterface("AbstractValidator")),
                verbose: true)
            .ThrowIfAnyFailures("Query Validator Rule");
    }

    [Fact]
    public void Query_ShouldHave_UsecaseNestedClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Query")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClass("Usecase", nested => nested
                    .RequireSealed()
                    .RequireImplementsGenericInterface("IQueryUsecase")),
                verbose: true)
            .ThrowIfAnyFailures("Query Usecase Rule");
    }
}
