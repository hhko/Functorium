using Functorium.Abstractions.Errors;

namespace Functorium.Domains.Errors;

/// <summary>
/// 도메인 에러 타입의 기본 record 클래스
/// sealed record 계층으로 타입 안전한 에러 정의 제공
/// </summary>
/// <remarks>
/// 사용 예시:
/// <code>
/// using static Functorium.Domains.Errors.DomainErrorKind;
///
/// DomainError.For&lt;Email&gt;(new Empty(), value, "Email cannot be empty");
/// DomainError.For&lt;Password&gt;(new TooShort(MinLength: 8), value, "Password too short");
/// // 커스텀 에러: sealed record 파생 정의
/// // public sealed record Unsupported : DomainErrorKind.Custom;
/// DomainError.For&lt;Currency&gt;(new Unsupported(), value, "Currency not supported");
/// </code>
/// </remarks>
public abstract partial record DomainErrorKind : ErrorKind;
