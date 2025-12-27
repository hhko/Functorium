using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using Framework.Layers.Domains;
using Framework.Test.ArchitectureRules;
using LanguageExt;
using LanguageExt.Common;
using Xunit;

namespace ArchitectureTest.Tests.Unit;

[Trait("Concept-14-Architecture-Test", "DomainRuleTests")]
public class DomainRuleTests : ArchitectureTestBase
{
    private readonly ITestOutputHelper _output;

    public DomainRuleTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ValueObject_ShouldSatisfy_Rules()
    {
        ArchRuleDefinition
            .Classes()
            .That()
            .ImplementInterface(typeof(IValueObject))       //.AreAssignableTo(types: [typeof(ValueObject)])
            .And()
            .AreNotAbstract()                               // abstract 클래스들(ValueObject, SimpleValueObject<T>) 제외
            .ValidateAllClasses(Architecture, @class =>
            {
                // 값 객체 클래스
                @class
                    .RequirePublic()
                    .RequireSealed()
                    .RequireAllPrivateConstructors()
                    .RequireImmutable()  // 불변성 검증 추가
                    .RequireMethod(IValueObject.CreateMethodName, method => method
                        .RequireVisibility(Visibility.Public)
                        .RequireStatic()
                        .RequireReturnType(typeof(Fin<>)))
                    .RequireMethod(IValueObject.CreateFromValidatedMethodName, method => method
                        .RequireVisibility(Visibility.Internal)
                        .RequireStatic()
                        .RequireReturnTypeOfDeclaringClass())
                    .RequireMethod(IValueObject.ValidateMethodName, method => method
                        .RequireVisibility(Visibility.Public)
                        .RequireStatic()
                        .RequireReturnType(typeof(Validation<,>)))
                    .RequireImplements(typeof(IEquatable<>));

                // 값 객체 중첩 클래스
                @class
                    .RequireNestedClassIfExists(IValueObject.DomainErrorsNestedClassName, domainErrors =>
                    {
                        domainErrors
                            .RequireInternal()
                            .RequireSealed()
                            .RequireAllMethods(method => method
                                .RequireVisibility(Visibility.Public)
                                .RequireStatic()
                                .RequireReturnType(typeof(Error)));
                    });
            }, _output)
            .ThrowIfAnyFailures("ValueObject Rule");

    }
}
