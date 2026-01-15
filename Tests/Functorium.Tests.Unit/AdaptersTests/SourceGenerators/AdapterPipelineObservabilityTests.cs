using Functorium.Adapters.Observabilities.Naming;
using Functorium.Adapters.SourceGenerator;
using Functorium.Testing.Actions.SourceGenerators;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

/// <summary>
/// AdapterPipelineGenerator가 생성하는 코드의 Observability 태그 구조를 검증하는 테스트입니다.
/// </summary>
/// <remarks>
/// <para>
/// 이 테스트는 생성된 코드가 README.md에 문서화된 태그 구조를 준수하는지 검증합니다.
/// </para>
/// <para>
/// Adapter Layer Metrics 태그 구조:
/// </para>
/// <code>
/// ┌──────────────────────────┬─────────────────────────┬─────────────────────────┬─────────────────────────┐
/// │ Tag Key                  │ requestCounter          │ responseCounter         │ responseCounter         │
/// │                          │ durationHistogram       │ (success)               │ (failure)               │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ request.layer            │ "adapter"               │ "adapter"               │ "adapter"               │
/// │ request.category         │ category name           │ category name           │ category name           │
/// │ request.handler          │ handler name            │ handler name            │ handler name            │
/// │ request.handler.method   │ method name             │ method name             │ method name             │
/// │ response.status          │ (none)                  │ "success"               │ "failure"               │
/// │ error.type               │ (none)                  │ (none)                  │ "expected"/"exceptional"│
/// │ error.code               │ (none)                  │ (none)                  │ error code              │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ Total Tags               │ 4                       │ 5                       │ 7                       │
/// └──────────────────────────┴─────────────────────────┴─────────────────────────┴─────────────────────────┘
/// </code>
/// <para>
/// Adapter Layer Tracing 태그 구조:
/// </para>
/// <code>
/// ┌──────────────────────────┬─────────────────────────┬─────────────────────────┐
/// │ Tag Key                  │ Success                 │ Failure                 │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ request.layer            │ "adapter"               │ "adapter"               │
/// │ request.category         │ category name           │ category name           │
/// │ request.handler          │ handler name            │ handler name            │
/// │ request.handler.method   │ method name             │ method name             │
/// │ response.elapsed         │ elapsed seconds         │ elapsed seconds         │
/// │ response.status          │ "success"               │ "failure"               │
/// │ error.type               │ (none)                  │ "expected"/"exceptional"│
/// │ error.code               │ (none)                  │ error code              │
/// ├──────────────────────────┼─────────────────────────┼─────────────────────────┤
/// │ Total Tags               │ 6                       │ 8                       │
/// └──────────────────────────┴─────────────────────────┴─────────────────────────┘
/// </code>
/// </remarks>
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters_SourceGenerator)]
public sealed class AdapterPipelineObservabilityTests
{
    private readonly AdapterPipelineGenerator _sut;

    public AdapterPipelineObservabilityTests()
    {
        _sut = new AdapterPipelineGenerator();
    }

    #region Metrics 태그 구조 검증

    /// <summary>
    /// 생성된 코드의 Request Metrics 태그가 올바른 키를 사용하는지 검증합니다.
    /// Request Counter는 4개 태그: request.layer, request.category, request.handler, request.handler.method
    /// </summary>
    [Fact]
    public void GeneratedCode_RequestMetrics_ShouldContainCorrectTagKeys()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // Request Counter 태그 구조 검증 (AcquireActivity 메서드 내)
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.RequestLayer, {nameof(ObservabilityNaming)}.Layers.Adapter }}");
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.RequestCategory, this.GetRequestCategoryPascalCase() }}");
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.RequestHandler, requestHandler }}");
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.RequestHandlerMethod, requestHandlerMethod }}");
    }

    /// <summary>
    /// 생성된 코드의 Response Success Metrics 태그가 올바른 키를 사용하는지 검증합니다.
    /// Response Counter (success)는 5개 태그: 4개 + response.status
    /// </summary>
    [Fact]
    public void GeneratedCode_ResponseSuccessMetrics_ShouldContainCorrectTagKeys()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // RecordActivitySuccess 메서드에서 response.status = "success" 확인
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.ResponseStatus, {nameof(ObservabilityNaming)}.Status.Success }}");
    }

    /// <summary>
    /// 생성된 코드의 Response Failure Metrics 태그가 올바른 키를 사용하는지 검증합니다.
    /// Response Counter (failure)는 7개 태그: 4개 + response.status + error.type + error.code
    /// </summary>
    [Fact]
    public void GeneratedCode_ResponseFailureMetrics_ShouldContainCorrectTagKeys()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // RecordActivityFailure 메서드에서 error 태그 확인
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.ResponseStatus, {nameof(ObservabilityNaming)}.Status.Failure }}");
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.OTelAttributes.ErrorType, errorType }}");
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.ErrorCode, errorCode }}");
    }

    /// <summary>
    /// 생성된 코드의 Metrics 이름이 올바른 패턴을 따르는지 검증합니다.
    /// 패턴: adapter.{category}.{requests|responses|duration}
    /// </summary>
    [Fact]
    public void GeneratedCode_MetricsNames_ShouldFollowCorrectPattern()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // Metrics 이름 패턴 검증
        generatedCode.ShouldContain("$\"adapter.{categoryLower}.requests\"");
        generatedCode.ShouldContain("$\"adapter.{categoryLower}.responses\"");
        generatedCode.ShouldContain("$\"adapter.{categoryLower}.duration\"");
    }

    #endregion

    #region Tracing 태그 구조 검증

    /// <summary>
    /// 생성된 코드의 Tracing Request 태그가 올바른 키를 사용하는지 검증합니다.
    /// Activity 생성 시 4개 태그: request.layer, request.category, request.handler, request.handler.method
    /// </summary>
    [Fact]
    public void GeneratedCode_TracingRequestTags_ShouldContainCorrectKeys()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // AcquireActivity 메서드 내 TagList 검증
        generatedCode.ShouldContain("TagList tags = new()");
        generatedCode.ShouldContain($"{{ {nameof(ObservabilityNaming)}.CustomAttributes.RequestLayer, {nameof(ObservabilityNaming)}.Layers.Adapter }}");
    }

    /// <summary>
    /// 생성된 코드의 Tracing Success 태그가 올바른 키를 사용하는지 검증합니다.
    /// Success 시 response.elapsed와 response.status 태그 추가
    /// </summary>
    [Fact]
    public void GeneratedCode_TracingSuccessTags_ShouldContainCorrectKeys()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // RecordActivitySuccess 메서드에서 Activity 태그 설정 확인
        generatedCode.ShouldContain($"activity?.SetTag({nameof(ObservabilityNaming)}.CustomAttributes.ResponseElapsed, elapsed);");
        generatedCode.ShouldContain($"activity?.SetTag({nameof(ObservabilityNaming)}.CustomAttributes.ResponseStatus, {nameof(ObservabilityNaming)}.Status.Success);");
        generatedCode.ShouldContain("activity?.SetStatus(ActivityStatusCode.Ok);");
    }

    /// <summary>
    /// 생성된 코드의 Tracing Failure 태그가 올바른 키를 사용하는지 검증합니다.
    /// Failure 시 response.elapsed, response.status, error.type, error.code 태그 추가
    /// </summary>
    [Fact]
    public void GeneratedCode_TracingFailureTags_ShouldContainCorrectKeys()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // RecordActivityFailure 메서드에서 Activity 태그 설정 확인
        generatedCode.ShouldContain($"activity?.SetTag({nameof(ObservabilityNaming)}.CustomAttributes.ResponseElapsed, elapsed);");
        generatedCode.ShouldContain($"activity?.SetTag({nameof(ObservabilityNaming)}.CustomAttributes.ResponseStatus, {nameof(ObservabilityNaming)}.Status.Failure);");
        generatedCode.ShouldContain($"activity?.SetTag({nameof(ObservabilityNaming)}.OTelAttributes.ErrorType, errorType);");
        generatedCode.ShouldContain($"activity?.SetTag({nameof(ObservabilityNaming)}.CustomAttributes.ErrorCode, errorCode);");
        generatedCode.ShouldContain("activity?.SetStatus(ActivityStatusCode.Error");
    }

    /// <summary>
    /// 생성된 코드의 Span 이름이 올바른 패턴을 따르는지 검증합니다.
    /// 패턴: {layer} {category} {handler}.{method}
    /// </summary>
    [Fact]
    public void GeneratedCode_SpanName_ShouldFollowCorrectPattern()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // Span 이름 생성 패턴 검증
        generatedCode.ShouldContain($"{nameof(ObservabilityNaming)}.Spans.OperationName(");
        generatedCode.ShouldContain($"{nameof(ObservabilityNaming)}.Layers.Adapter,");
    }

    #endregion

    #region Error 처리 검증

    /// <summary>
    /// 생성된 코드가 Expected/Exceptional/Aggregate 에러 타입을 올바르게 분류하는지 검증합니다.
    /// </summary>
    [Fact]
    public void GeneratedCode_ErrorHandling_ShouldClassifyErrorTypes()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // GetErrorInfo 메서드에서 에러 타입 분류 확인
        generatedCode.ShouldContain($"{nameof(ObservabilityNaming)}.ErrorTypes.Aggregate");
        generatedCode.ShouldContain($"{nameof(ObservabilityNaming)}.ErrorTypes.Exceptional");
        generatedCode.ShouldContain($"{nameof(ObservabilityNaming)}.ErrorTypes.Expected");
    }

    /// <summary>
    /// 생성된 코드가 ManyErrors에서 대표 에러 코드를 올바르게 선정하는지 검증합니다.
    /// </summary>
    [Fact]
    public void GeneratedCode_ManyErrors_ShouldSelectPrimaryErrorCode()
    {
        // Arrange
        string input = CreateSimpleAdapterInput();

        // Act
        string? generatedCode = _sut.Generate(input);

        // Assert
        generatedCode.ShouldNotBeNull();

        // GetPrimaryErrorCode 메서드 존재 확인
        generatedCode.ShouldContain("GetPrimaryErrorCode(");
        generatedCode.ShouldContain("ManyErrors");
    }

    #endregion

    #region Helper Methods

    private static string CreateSimpleAdapterInput()
    {
        return """
            using Functorium.Adapters.SourceGenerator;
            using Functorium.Applications.Observabilities;
            using LanguageExt;

            namespace TestNamespace;

            public interface ITestAdapter : IAdapter
            {
                FinT<IO, int> GetValue();
            }

            [GeneratePipeline]
            public class TestAdapter : ITestAdapter
            {
                public string RequestCategory => "Test";
                public virtual FinT<IO, int> GetValue() => FinT<IO, int>.Succ(42);
            }
            """;
    }

    #endregion
}
