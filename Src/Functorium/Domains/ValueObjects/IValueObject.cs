namespace Functorium.Domains.ValueObjects;

/// <summary>
/// ValueObject의 기본 마커 인터페이스. 아키텍처 테스트에서 ValueObject 구현체를
/// 식별하는 경계선 역할을 합니다. 네이밍 규약 상수는 <see cref="ArchTestContract"/>에
/// 정의되어 있습니다.
/// </summary>
public interface IValueObject
{
    /// <summary>
    /// 아키텍처 테스트(ArchUnitNET 규약 스위트)가 모든 ValueObject 구현체에 대해
    /// enforce하는 네이밍 계약. 이 상수들은 <c>RequireMethod(...)</c>·
    /// <c>RequireNestedClassIfExists(...)</c>에 인자로 전달되며, 프로덕션 로직은
    /// 참조하지 않습니다. 값이나 상수 이름을 변경할 때는 반드시 대응 아키테스트
    /// 기대값을 함께 업데이트하십시오.
    /// </summary>
    public static class ArchTestContract
    {
        /// <summary>새 인스턴스를 생성하는 정적 팩토리 메서드 이름.</summary>
        public const string CreateMethodName = "Create";

        /// <summary>검증 완료된 데이터로 복원하는 정적 팩토리 메서드 이름 (Repository/ORM 경로).</summary>
        public const string CreateFromValidatedMethodName = "CreateFromValidated";

        /// <summary>입력 검증을 수행하는 정적 메서드 이름 (Validation&lt;Error, T&gt; 반환).</summary>
        public const string ValidateMethodName = "Validate";

        /// <summary>
        /// ValueObject 내부에 에러 코드 상수를 담는 nested 클래스 이름.
        /// 레이어 prefix 값(<see cref="Functorium.Abstractions.Errors.ErrorCodePrefixes.Domain"/>)과
        /// 동기화되어 있습니다.
        /// </summary>
        public const string NestedErrorsClassName = "Domain";
    }
}
