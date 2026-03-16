using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class DtoArchitectureRuleTests
{
    [Fact]
    public void PersistenceMapper_ShouldBe_InternalStaticWithMethods()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .And().HaveNameContaining("Mapper")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireInternal()
                .RequireStatic()
                .RequireMethod("ToModel", m => m.RequireStatic().RequireExtensionMethod())
                .RequireMethod("ToDomain", m => m.RequireStatic().RequireExtensionMethod()),
                verbose: true)
            .ThrowIfAnyFailures("Persistence Mapper Rule");
    }

    [Fact]
    public void PersistenceModel_ShouldBe_PublicPocoClass()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ArchitectureTestBase.PersistenceNamespace)
            .And().HaveNameEndingWith("Model")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequirePublic()
                .RequireNotSealed()
                .RequireOnlyPrimitiveProperties(),
                verbose: true)
            .ThrowIfAnyFailures("Persistence Model Rule");
    }

    [Fact]
    public void ApplicationUsecase_ShouldHave_NestedRequestResponse()
    {
        Action<ClassValidator> commandRule = @class => @class
            .RequireSealed()
            .RequireNestedClass("Request", nested => nested
                .RequireSealed().RequireRecord()
                .RequireOnlyPrimitiveProperties()
                .RequireImplementsGenericInterface("ICommandRequest"))
            .RequireNestedClass("Response", nested => nested
                .RequireSealed().RequireRecord()
                .RequireOnlyPrimitiveProperties("LanguageExt.Seq"));

        Action<ClassValidator> queryRule = @class => @class
            .RequireSealed()
            .RequireNestedClass("Request", nested => nested
                .RequireSealed().RequireRecord()
                .RequireOnlyPrimitiveProperties()
                .RequireImplementsGenericInterface("IQueryRequest"))
            .RequireNestedClass("Response", nested => nested
                .RequireSealed().RequireRecord()
                .RequireOnlyPrimitiveProperties("LanguageExt.Seq"));

        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ArchitectureTestBase.ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Command")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, commandRule, verbose: true)
            .ThrowIfAnyFailures("Application Command DTO Rule");

        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ArchitectureTestBase.ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Query")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, queryRule, verbose: true)
            .ThrowIfAnyFailures("Application Query DTO Rule");
    }

    [Fact]
    public void SharedApplicationDto_ShouldBe_SealedRecordWithPrimitives()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ArchitectureTestBase.ApplicationNamespace)
            .And().HaveFullNameContaining(".Dtos.")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireSealed()
                .RequireRecord()
                .RequireOnlyPrimitiveProperties(),
                verbose: true)
            .ThrowIfAnyFailures("Shared Application DTO Rule");
    }

    [Fact]
    public void PresentationEndpoint_ShouldHave_SealedRecordDtos()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ArchitectureTestBase.PresentationNamespace}.Endpoints")
            .And().HaveNameEndingWith("Endpoint")
            .ValidateAllClasses(ArchitectureTestBase.Architecture, @class => @class
                .RequireSealed()
                .RequireNestedClassIfExists("Request", nested => nested
                    .RequireSealed().RequireRecord()
                    .RequireOnlyPrimitiveProperties())
                .RequireNestedClassIfExists("Response", nested => nested
                    .RequireSealed().RequireRecord()
                    .RequireOnlyPrimitiveProperties()),
                verbose: true)
            .ThrowIfAnyFailures("Presentation Endpoint DTO Rule");
    }
}
