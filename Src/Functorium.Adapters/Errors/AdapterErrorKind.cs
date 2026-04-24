using Functorium.Abstractions.Errors;

namespace Functorium.Adapters.Errors;

/// <summary>
/// 어댑터 레이어 에러 타입
/// sealed record 계층으로 타입 안전한 에러 정의 제공
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Adapters.Errors.AdapterErrorKind;
///
/// AdapterError.For&lt;UsecaseValidationPipeline&gt;(new PipelineValidation("PropertyName"), value, "Validation failed");
/// AdapterError.FromException&lt;UsecaseExceptionPipeline&gt;(new PipelineException(), exception);
/// </code>
/// </remarks>
public abstract partial record AdapterErrorKind : ErrorKind;
