using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace LayeredArch.Tests.Unit.Architecture;

/// <summary>
/// CQRS 패턴 준수 검증: Query 유스케이스가 IRepository에 의존하지 않도록 강제.
/// </summary>
public sealed class CqrsArchitectureRuleTests : ArchitectureTestBase
{
    [Fact]
    public void QueryUsecase_ShouldNotDependOn_IRepository()
    {
        // Query 유스케이스의 Usecase 클래스가 IRepository 파생 인터페이스에 의존하면 안 됨.
        // IQueryAdapter 기반 포트를 사용해야 합니다.
        Classes()
            .That()
            .HaveFullNameContaining("Query+Usecase")
            .Should().NotDependOnAnyTypesThat()
            .HaveNameEndingWith("Repository")
            .Check(Architecture);
    }
}
