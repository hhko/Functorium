using Functorium.SourceGenerators.Generators.UnionTypeGenerator;
using Functorium.Testing.Actions.SourceGenerators;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

// # UnionTypeGenerator 소스 생성기 테스트
//
// [UnionType] 속성이 붙은 abstract partial record에 대해
// Match/Switch 메서드가 올바르게 생성되는지 검증합니다.
//
// ## 테스트 시나리오
//
// ### 1. Match 메서드 생성 테스트
// - 모든 케이스에 대한 Func 파라미터 생성
// - TResult 제네릭 반환 타입
//
// ### 2. Switch 메서드 생성 테스트
// - 모든 케이스에 대한 Action 파라미터 생성
// - void 반환
//
// ### 3. 파라미터 이름 변환 테스트
// - PascalCase → camelCase 변환
//
// ### 4. UnreachableCaseException 사용 테스트
// - 기본 분기에 UnreachableCaseException 포함
//
[Trait(nameof(UnitTest), UnitTest.Functorium_SourceGenerator)]
public sealed class UnionTypeGeneratorTests
{
    private readonly UnionTypeGenerator _sut;

    public UnionTypeGeneratorTests()
    {
        _sut = new UnionTypeGenerator();
    }

    #region 1. Match 메서드 생성 테스트

    /// <summary>
    /// 시나리오: [UnionType] 속성이 붙은 abstract partial record에 대해
    /// 모든 케이스의 Func 파라미터를 포함하는 Match 메서드가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldGenerate_MatchMethod()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public TResult Match<TResult>(");
        actual.ShouldContain("global::System.Func<Circle, TResult> circle");
        actual.ShouldContain("global::System.Func<Rectangle, TResult> rectangle");
    }

    /// <summary>
    /// 시나리오: Match 메서드의 switch 식에서 각 케이스가 올바르게 매핑되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_MatchMethod_ShouldContain_CaseMapping()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("Circle __case => circle(__case)");
        actual.ShouldContain("Rectangle __case => rectangle(__case)");
    }

    #endregion

    #region 2. Switch 메서드 생성 테스트

    /// <summary>
    /// 시나리오: [UnionType] 속성이 붙은 abstract partial record에 대해
    /// 모든 케이스의 Action 파라미터를 포함하는 Switch 메서드가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldGenerate_SwitchMethod()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public void Switch(");
        actual.ShouldContain("global::System.Action<Circle> circle");
        actual.ShouldContain("global::System.Action<Rectangle> rectangle");
    }

    /// <summary>
    /// 시나리오: Switch 메서드의 switch 문에서 각 케이스가 올바르게 매핑되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_SwitchMethod_ShouldContain_CaseMapping()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("case Circle __case: circle(__case); break;");
        actual.ShouldContain("case Rectangle __case: rectangle(__case); break;");
    }

    #endregion

    #region 3. 파라미터 이름 변환 테스트

    /// <summary>
    /// 시나리오: 케이스 이름이 PascalCase에서 camelCase로 변환되어 파라미터 이름이 되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldConvert_CaseNames_ToCamelCase()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record ContactInfo : UnionValueObject
            {
                public sealed record EmailOnly(string Email) : ContactInfo;
                public sealed record PostalOnly(string Address) : ContactInfo;
                public sealed record EmailAndPostal(string Email, string Address) : ContactInfo;
                private ContactInfo() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("emailOnly");
        actual.ShouldContain("postalOnly");
        actual.ShouldContain("emailAndPostal");
    }

    #endregion

    #region 4. UnreachableCaseException 사용 테스트

    /// <summary>
    /// 시나리오: 생성된 Match/Switch 코드의 기본 분기에 UnreachableCaseException이 사용되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldUse_UnreachableCaseException()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("global::Functorium.Domains.ValueObjects.Unions.UnreachableCaseException");
    }

    #endregion

    #region 5. 네임스페이스 생성 테스트

    /// <summary>
    /// 시나리오: 올바른 네임스페이스가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldGenerate_CorrectNamespace()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace MyApp.Domain.ValueObjects;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("namespace MyApp.Domain.ValueObjects;");
    }

    #endregion

    #region 6. 3개 이상 케이스 테스트

    /// <summary>
    /// 시나리오: 3개 이상의 케이스가 있는 union에서 모든 케이스가 포함되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldHandle_MultipleCases()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Result : UnionValueObject
            {
                public sealed record Success(string Value) : Result;
                public sealed record Warning(string Value, string Message) : Result;
                public sealed record Failure(string Error) : Result;
                private Result() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("global::System.Func<Success, TResult> success");
        actual.ShouldContain("global::System.Func<Warning, TResult> warning");
        actual.ShouldContain("global::System.Func<Failure, TResult> failure");
        actual.ShouldContain("global::System.Action<Success> success");
        actual.ShouldContain("global::System.Action<Warning> warning");
        actual.ShouldContain("global::System.Action<Failure> failure");
    }

    #endregion

    #region 8. Is{CaseName} 속성 생성 테스트

    /// <summary>
    /// 시나리오: 각 케이스에 대한 Is{CaseName} 속성이 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldGenerate_IsProperties()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public bool IsCircle => this is Circle;");
        actual.ShouldContain("public bool IsRectangle => this is Rectangle;");
    }

    #endregion

    #region 9. As{CaseName}() 메서드 생성 테스트

    /// <summary>
    /// 시나리오: 각 케이스에 대한 As{CaseName}() 메서드가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void UnionTypeGenerator_ShouldGenerate_AsMethods()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public Circle? AsCircle() => this as Circle;");
        actual.ShouldContain("public Rectangle? AsRectangle() => this as Rectangle;");
    }

    #endregion

    #region 7. 스냅샷 테스트

    /// <summary>
    /// 시나리오: 생성된 코드의 전체 형태를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public Task UnionTypeGenerator_ShouldGenerate_ExpectedOutput()
    {
        // Arrange
        string input = """
            using Functorium.Domains.ValueObjects.Unions;

            namespace TestNamespace;

            [UnionType]
            public abstract partial record Shape : UnionValueObject
            {
                public sealed record Circle(double Radius) : Shape;
                public sealed record Rectangle(double Width, double Height) : Shape;
                private Shape() { }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/UnionTypeGenerator");
    }

    #endregion
}
