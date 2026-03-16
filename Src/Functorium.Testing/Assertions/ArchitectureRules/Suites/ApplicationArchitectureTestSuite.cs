using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;

namespace Functorium.Testing.Assertions.ArchitectureRules.Suites;

public abstract class ApplicationArchitectureTestSuite
{
    protected abstract Architecture Architecture { get; }
    protected abstract string ApplicationNamespace { get; }

    /// <summary>
    /// Command에 Validator 중첩 클래스가 있으면 sealed이고 AbstractValidator를 구현해야 합니다.
    /// FluentValidation 기반 입력 유효성 검증 구조를 강제합니다.
    /// </summary>
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

    /// <summary>
    /// Command에 Usecase 중첩 클래스가 필수입니다.
    /// sealed이고 ICommandUsecase를 구현하여 CQRS 커맨드 처리 구조를 보장합니다.
    /// </summary>
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

    /// <summary>
    /// Query에 Validator 중첩 클래스가 있으면 sealed이고 AbstractValidator를 구현해야 합니다.
    /// FluentValidation 기반 입력 유효성 검증 구조를 강제합니다.
    /// </summary>
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

    /// <summary>
    /// Query에 Usecase 중첩 클래스가 필수입니다.
    /// sealed이고 IQueryUsecase를 구현하여 CQRS 쿼리 처리 구조를 보장합니다.
    /// </summary>
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
