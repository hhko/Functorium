using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace NestedClassValidation.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(NestedClassValidation.Applications.CreateOrder).Assembly)
            .Build();

    protected static readonly string ApplicationNamespace =
        typeof(NestedClassValidation.Applications.CreateOrder).Namespace!;
}

[Trait("Part3-Advanced-Validation", "NestedClassValidation")]
public sealed class NestedClassArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void CommandClasses_ShouldHave_SealedRequestAndResponse()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And()
            .AreNotNested()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClass("Request", nested => nested
                    .RequireSealed())
                .RequireNestedClass("Response", nested => nested
                    .RequireSealed()),
                verbose: true)
            .ThrowIfAnyFailures("Command Nested Class Rule");
    }

    [Fact]
    public void CommandClasses_ShouldHave_ImmutableNestedClasses()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And()
            .AreNotNested()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClass("Request", nested => nested
                    .RequireSealed()
                    .RequireImmutable())
                .RequireNestedClass("Response", nested => nested
                    .RequireSealed()
                    .RequireImmutable()),
                verbose: true)
            .ThrowIfAnyFailures("Command Nested Immutability Rule");
    }

    [Fact]
    public void CommandClasses_ShouldOptionallyHave_Validator()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And()
            .AreNotNested()
            .ValidateAllClasses(Architecture, @class => @class
                .RequireNestedClassIfExists("Validator", nested => nested
                    .RequireSealed()),
                verbose: true)
            .ThrowIfAnyFailures("Optional Validator Nested Class Rule");
    }
}
