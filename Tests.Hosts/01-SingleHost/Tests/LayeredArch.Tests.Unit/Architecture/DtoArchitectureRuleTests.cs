using ArchUnitNET.Fluent;
using Functorium.Testing.Assertions.ArchitectureRules;

namespace LayeredArch.Tests.Unit.Architecture;

public sealed class DtoArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void PersistenceMapper_ShouldBe_InternalStaticWithMethods()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(PersistenceNamespace)
            .And().HaveNameContaining("Mapper")
            .ValidateAllClasses(Architecture, @class => @class
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
            .ResideInNamespace(PersistenceNamespace)
            .And().HaveNameEndingWith("Model")
            .ValidateAllClasses(Architecture, @class => @class
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
            .ResideInNamespace($"{ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Command")
            .ValidateAllClasses(Architecture, commandRule, verbose: true)
            .ThrowIfAnyFailures("Application Command DTO Rule");

        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace($"{ApplicationNamespace}.Usecases")
            .And().HaveNameEndingWith("Query")
            .ValidateAllClasses(Architecture, queryRule, verbose: true)
            .ThrowIfAnyFailures("Application Query DTO Rule");
    }

    [Fact]
    public void SharedApplicationDto_ShouldBe_SealedRecordWithPrimitives()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And().HaveFullNameContaining(".Dtos.")
            .ValidateAllClasses(Architecture, @class => @class
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
            .ResideInNamespace($"{PresentationNamespace}.Endpoints")
            .And().HaveNameEndingWith("Endpoint")
            .ValidateAllClasses(Architecture, @class => @class
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
