namespace Functorium.Abstractions.Errors;

/// <summary>
/// 레이어별 에러 코드 접두사 상수. 에러 코드는 "{Prefix}.{Context}.{Name}"
/// 형식으로 조립되며, 이 상수들은 레이어 팩토리(<c>DomainError</c>·
/// <c>ApplicationError</c>·<c>AdapterError</c>)와 대응 테스트 어설션에서만
/// 사용됩니다.
/// </summary>
/// <remarks>
/// <para>
/// 공개 노출되지 않는 이유: 외부 소비자(사용자 코드)는 <c>DomainError.For&lt;T&gt;(...)</c>
/// 같은 레이어 팩토리만 호출하며, prefix 문자열 자체에 직접 의존하지 않습니다.
/// 상수 자체를 internal로 유지해 공개 API 표면을 축소하고, 값 변경의 파급
/// 범위를 <c>InternalsVisibleTo</c> 경계 안으로 제한합니다.
/// </para>
/// <para>
/// 1.0.0-alpha.4 이후: 기존 "DomainErrors" / "ApplicationErrors" / "AdapterErrors" 값을
/// "Domain" / "Application" / "Adapter"로 단축했습니다. 에러 코드가 짧아져
/// 대시보드·알림에서 읽기 편해지고, 컨텍스트(<c>{Context}</c>)와의 역할 구분이
/// 명확해집니다.
/// </para>
/// </remarks>
internal static class ErrorCodePrefixes
{
    /// <summary>도메인 레이어 에러 코드 접두사.</summary>
    public const string Domain = "Domain";

    /// <summary>애플리케이션 레이어 에러 코드 접두사.</summary>
    public const string Application = "Application";

    /// <summary>어댑터 레이어 에러 코드 접두사.</summary>
    public const string Adapter = "Adapter";
}
