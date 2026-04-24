using Functorium.Abstractions.Errors;

namespace Functorium.Applications.Errors;

/// <summary>
/// 애플리케이션 레이어 에러 타입
/// sealed record 계층으로 타입 안전한 에러 정의 제공
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Applications.Errors.ApplicationErrorKind;
///
/// ApplicationError.For&lt;CreateProductCommand&gt;(new AlreadyExists(), productId, "Product already exists");
/// ApplicationError.For&lt;UpdateOrderCommand&gt;(new ValidationFailed("Quantity"), value, "Quantity must be positive");
/// </code>
/// </remarks>
public abstract partial record ApplicationErrorKind : ErrorKind;
