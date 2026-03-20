using Functorium.SourceGenerators.Generators.LogEnricherGenerator;
using Functorium.Testing.Actions.SourceGenerators;

using Microsoft.CodeAnalysis;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

// # LogEnricherGenerator 소스 생성기 테스트
//
// ICommandRequest<T> / IQueryRequest<T> 구현체를 자동 감지하여
// IUsecaseLogEnricher 구현체가 올바르게 생성되는지 검증합니다.
// [LogEnricherIgnore]를 Request record에 적용하면 생성을 제외합니다.
//
// ## 테스트 시나리오
//
// ### 1. Request 스칼라 속성 PushProperty 테스트
// ### 2. Response 스칼라 속성 PushProperty 테스트 (FinResponse.Succ 패턴 매칭)
// ### 3. 컬렉션 .Count 테스트
// ### 4. [LogEnricherIgnore] 제외 테스트
// ### 5. 복합 타입 건너뜀 테스트
// ### 6. partial void 확장 포인트 존재 테스트
// ### 7. 스냅샷 테스트
// ### 8. ctx 필드 타입 충돌 감지 테스트 (FUNCTORIUM002)
// ### 9a. PushRequestCtx/PushResponseCtx 헬퍼 메서드 테스트
// ### 14. [LogEnricherIgnore] 클래스 레벨 옵트아웃 테스트
// ### 15. 접근 불가능한 타입 진단 경고 테스트 (FUNCTORIUM003)
//

[Trait(nameof(UnitTest), UnitTest.Functorium_SourceGenerator)]
public sealed class LogEnricherGeneratorTests
{
    private readonly LogEnricherGenerator _sut;

    public LogEnricherGeneratorTests()
    {
        _sut = new LogEnricherGenerator();
    }

    private const string CommonInput = """
        using System.Collections.Generic;
        using Functorium.Applications.Usecases;

        namespace TestNamespace;

        public sealed class PlaceOrderCommand
        {
            public sealed record OrderLine(string ProductId, int Quantity, decimal UnitPrice);

            public sealed record Request(string CustomerId, List<OrderLine> Lines) : ICommandRequest<Response>;

            public sealed record Response(string OrderId, int LineCount, decimal TotalAmount);
        }
        """;

    #region 1. Request 스칼라 속성 PushProperty 테스트

    /// <summary>
    /// 시나리오: Request의 스칼라 속성(string)이 ctx.snake_case로 PushProperty되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_RequestScalarProperty()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("PushProperty(\"ctx.place_order_command.request.customer_id\", request.CustomerId)");
    }

    #endregion

    #region 2. Response 스칼라 속성 PushProperty 테스트

    /// <summary>
    /// 시나리오: Response의 스칼라 속성이 FinResponse.Succ 패턴 매칭으로 PushProperty되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_ResponseScalarProperties()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("PushProperty(\"ctx.place_order_command.response.order_id\", r.OrderId)");
        actual.ShouldContain("PushProperty(\"ctx.place_order_command.response.line_count\", r.LineCount)");
        actual.ShouldContain("PushProperty(\"ctx.place_order_command.response.total_amount\", r.TotalAmount)");
    }

    /// <summary>
    /// 시나리오: Response 추출 시 FinResponse.Succ 패턴 매칭이 사용되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldUse_FinResponseSuccPatternMatching()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain(".Succ { Value: var r }");
    }

    #endregion

    #region 3. 컬렉션 .Count 테스트

    /// <summary>
    /// 시나리오: 컬렉션 속성에 대해 _count 접미사와 .Count 표현식이 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_CollectionCountProperty()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("PushProperty(\"ctx.place_order_command.request.lines_count\", request.Lines?.Count ?? 0)");
    }

    #endregion

    #region 4. [LogEnricherIgnore] 제외 테스트

    /// <summary>
    /// 시나리오: [LogEnricherIgnore] 속성이 붙은 프로퍼티는 생성에서 제외되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldExclude_IgnoredProperties()
    {
        // Arrange
        string input = """
            using System.Collections.Generic;
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class TestCommand
            {
                public sealed record Request(
                    string CustomerId,
                    [LogEnricherIgnore] string InternalCorrelationId,
                    int Amount) : ICommandRequest<Response>;

                public sealed record Response(string OrderId);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("ctx.test_command.request.customer_id");
        actual.ShouldContain("ctx.test_command.request.amount");
        actual.ShouldNotContain("correlation");
        actual.ShouldNotContain("InternalCorrelationId");
    }

    #endregion

    #region 5. 복합 타입 건너뜀 테스트

    /// <summary>
    /// 시나리오: 복합 타입(클래스/레코드) 속성은 자동 생성에서 건너뛰는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldSkip_ComplexTypeProperties()
    {
        // Arrange
        string input = """
            using System.Collections.Generic;
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class TestCommand
            {
                public sealed record Address(string Street, string City);

                public sealed record Request(
                    string CustomerId,
                    Address ShippingAddress) : ICommandRequest<Response>;

                public sealed record Response(string OrderId);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("ctx.test_command.request.customer_id");
        actual.ShouldNotContain("shipping_address");
        actual.ShouldNotContain("ShippingAddress");
    }

    #endregion

    #region 6. partial void 확장 포인트 존재 테스트

    /// <summary>
    /// 시나리오: OnEnrichRequestLog/OnEnrichResponseLog partial void 메서드가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_PartialVoidExtensionPoints()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("partial void OnEnrichRequestLog(");
        actual.ShouldContain("partial void OnEnrichResponseLog(");
    }

    #endregion

    #region 7. Enricher 클래스 이름 테스트

    /// <summary>
    /// 시나리오: Enricher 클래스 이름이 {ContainingTypes}{RequestTypeName}LogEnricher 형식인지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_CorrectClassName()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public partial class PlaceOrderCommandRequestLogEnricher");
    }

    #endregion

    #region 8. IUsecaseLogEnricher 인터페이스 구현 테스트

    /// <summary>
    /// 시나리오: 생성된 클래스가 IUsecaseLogEnricher&lt;Request, FinResponse&lt;Response&gt;&gt;를 구현하는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldImplement_IUsecaseLogEnricherInterface()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("IUsecaseLogEnricher<");
        actual.ShouldContain("FinResponse<");
    }

    #endregion

    #region 9. GeneratedCompositeDisposable 테스트

    /// <summary>
    /// 시나리오: GeneratedCompositeDisposable 내부 클래스가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_CompositeDisposable()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("GeneratedCompositeDisposable");
        actual.ShouldContain("public void Dispose()");
    }

    #endregion

    #region 9a. PushRequestCtx / PushResponseCtx 헬퍼 메서드 테스트

    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_PushRequestCtxHelper()
    {
        string? actual = _sut.Generate(CommonInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("private static void PushRequestCtx(");
        actual.ShouldContain("\"ctx.place_order_command.request.\" + fieldName, value)");
    }

    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_PushResponseCtxHelper()
    {
        string? actual = _sut.Generate(CommonInput);

        actual.ShouldNotBeNull();
        actual.ShouldContain("private static void PushResponseCtx(");
        actual.ShouldContain("\"ctx.place_order_command.response.\" + fieldName, value)");
    }

    #endregion

    #region 10. 네임스페이스 생성 테스트

    /// <summary>
    /// 시나리오: 올바른 네임스페이스가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldGenerate_CorrectNamespace()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("namespace TestNamespace;");
    }

    #endregion

    #region 11. Query Request 테스트

    /// <summary>
    /// 시나리오: IQueryRequest를 구현하는 Request에 대해서도 올바르게 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldHandle_QueryRequest()
    {
        // Arrange
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class GetOrderQuery
            {
                public sealed record Request(string OrderId) : IQueryRequest<Response>;

                public sealed record Response(string CustomerName, decimal Total);
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("ctx.get_order_query.request.order_id");
        actual.ShouldContain("ctx.get_order_query.response.customer_name");
        actual.ShouldContain("ctx.get_order_query.response.total");
        actual.ShouldContain("GetOrderQueryRequestLogEnricher");
    }

    #endregion

    #region 12. ctx 필드 타입 충돌 감지 테스트 (FUNCTORIUM002)

    /// <summary>
    /// 시나리오: 네임스페이스 격리로 서로 다른 커맨드의 동일 이름 필드가 충돌하지 않는지 확인합니다.
    /// PlaceOrderCommand.Response.OrderId (string) → ctx.place_order_command.response.order_id (keyword)
    /// CancelOrderCommand.Request.OrderId (int) → ctx.cancel_order_command.request.order_id (long)
    /// 서로 다른 ctx 필드명이므로 FUNCTORIUM002가 발생하지 않습니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldNotReportDiagnostic_WhenNamespaceIsolatesFields()
    {
        // Arrange
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class PlaceOrderCommand
            {
                public sealed record Request(string CustomerId) : ICommandRequest<Response>;
                public sealed record Response(string OrderId);
            }

            public sealed class CancelOrderCommand
            {
                public sealed record Request(int OrderId) : ICommandRequest<Response>;
                public sealed record Response(bool Cancelled);
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldNotContain(d => d.Id == "FUNCTORIUM002");
    }

    /// <summary>
    /// 시나리오: 같은 ctx 필드명이지만 같은 OpenSearch 매핑 타입 그룹이면
    /// FUNCTORIUM002 Warning이 발생하지 않는지 확인합니다.
    /// CommandA.Response.OrderId (string) → ctx.order_id (keyword)
    /// CommandB.Request.OrderId (string) → ctx.order_id (keyword)
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldNotReportDiagnostic_WhenSameTypeGroup()
    {
        // Arrange
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class PlaceOrderCommand
            {
                public sealed record Request(string CustomerId) : ICommandRequest<Response>;
                public sealed record Response(string OrderId);
            }

            public sealed class CancelOrderCommand
            {
                public sealed record Request(string OrderId) : ICommandRequest<Response>;
                public sealed record Response(bool Cancelled);
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldNotContain(d => d.Id == "FUNCTORIUM002");
    }

    /// <summary>
    /// 시나리오: 같은 OpenSearch 매핑 그룹 내 다른 C# 타입 (int vs long → 둘 다 "long" 그룹)이면
    /// 충돌로 판정하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldNotReportDiagnostic_WhenDifferentCSharpTypes_SameOpenSearchGroup()
    {
        // Arrange
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class PlaceOrderCommand
            {
                public sealed record Request(int Amount) : ICommandRequest<Response>;
                public sealed record Response(string OrderId);
            }

            public sealed class CancelOrderCommand
            {
                public sealed record Request(long Amount) : ICommandRequest<Response>;
                public sealed record Response(bool Cancelled);
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldNotContain(d => d.Id == "FUNCTORIUM002");
    }

    #endregion

    #region 14. [LogEnricherIgnore] 클래스 레벨 옵트아웃 테스트

    /// <summary>
    /// 시나리오: [LogEnricherIgnore]가 Request record에 적용되면 생성하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldNotGenerate_WhenClassLevelIgnoreApplied()
    {
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class InternalCommand
            {
                [LogEnricherIgnore]
                public sealed record Request(string Secret) : ICommandRequest<Response>;
                public sealed record Response(string Result);
            }
            """;

        string? actual = _sut.Generate(input);

        actual.ShouldBeNull();
    }

    #endregion

    #region 15. 접근 불가능한 타입 진단 경고 테스트 (FUNCTORIUM003)

    /// <summary>
    /// 시나리오: private Request record가 ICommandRequest를 구현하면
    /// FUNCTORIUM003 경고가 발생하는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldReportDiagnostic_WhenRequestTypeIsInaccessible()
    {
        // Arrange
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class OuterClass
            {
                private sealed record Request(string Value) : ICommandRequest<Response>;
                public sealed record Response(string Result);
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldContain(d => d.Id == "FUNCTORIUM003");
    }

    /// <summary>
    /// 시나리오: [LogEnricherIgnore]가 적용된 private Request record에 대해서는
    /// FUNCTORIUM003 경고가 발생하지 않는지 확인합니다.
    /// </summary>
    [Fact]
    public void LogEnricherGenerator_ShouldNotReportDiagnostic_WhenInaccessibleButIgnored()
    {
        // Arrange
        string input = """
            using Functorium.Applications.Usecases;

            namespace TestNamespace;

            public sealed class OuterClass
            {
                [LogEnricherIgnore]
                private sealed record Request(string Value) : ICommandRequest<Response>;
                public sealed record Response(string Result);
            }
            """;

        // Act
        var (_, diagnostics) = _sut.GenerateWithDiagnostics(input);

        // Assert
        diagnostics.ShouldNotContain(d => d.Id == "FUNCTORIUM003");
    }

    #endregion

    #region 16. 스냅샷 테스트 (기존 12번)

    /// <summary>
    /// 시나리오: 생성된 코드의 전체 형태를 스냅샷으로 검증합니다.
    /// </summary>
    [Fact]
    public Task LogEnricherGenerator_ShouldGenerate_ExpectedOutput()
    {
        // Act
        string? actual = _sut.Generate(CommonInput);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/LogEnricherGenerator");
    }

    #endregion
}
