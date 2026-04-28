using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using Functorium.Testing.Assertions.ArchitectureRules;
using Xunit;

namespace ImmutabilityRule.Tests.Unit;

public abstract class ArchitectureTestBase
{
    protected static readonly Architecture Architecture =
        new ArchLoader()
            .LoadAssemblies(typeof(ImmutabilityRule.Domains.Temperature).Assembly)
            .Build();

    protected static readonly string DomainNamespace =
        typeof(ImmutabilityRule.Domains.Temperature).Namespace!;
}

/// <summary>
/// ImmutabilityRule을 사용하여 도메인 클래스의 불변성을 검증합니다.
///
/// RequireImmutable()은 다음 6가지 차원에서 검증합니다:
/// 1. 기본 Writability 검증 - 멤버가 immutable인지 확인
/// 2. 생성자 검증 - 모든 생성자가 private인지 확인
/// 3. 프로퍼티 검증 - public setter가 없는지 확인
/// 4. 필드 검증 - public 필드가 없는지 확인
/// 5. 가변 컬렉션 타입 검증 - List, Dictionary 등 가변 컬렉션 사용 금지
/// 6. 상태 변경 메서드 검증 - 허용된 메서드(팩토리, getter, ToString 등) 외 금지
/// </summary>
[Trait("Part3-Advanced-Validation", "ImmutabilityRule")]
public sealed class ImmutabilityArchitectureTests : ArchitectureTestBase
{
    [Fact]
    public void DomainClasses_ShouldBe_Immutable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .ValidateAllClasses(Architecture, @class => @class
                .RequireImmutable(),
                verbose: true)
            .ThrowIfAnyFailures("Domain Immutability Rule");
    }

    [Fact]
    public void Temperature_ShouldBe_SealedAndImmutable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveName("Temperature")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed()
                .RequireImmutable(),
                verbose: true)
            .ThrowIfAnyFailures("Temperature Sealed Immutability Rule");
    }

    [Fact]
    public void Palette_ShouldBe_SealedAndImmutable()
    {
        ArchRuleDefinition.Classes()
            .That()
            .ResideInNamespace(DomainNamespace)
            .And()
            .HaveName("Palette")
            .ValidateAllClasses(Architecture, @class => @class
                .RequireSealed()
                .RequireImmutable(),
                verbose: true)
            .ThrowIfAnyFailures("Palette Sealed Immutability Rule");
    }
}
