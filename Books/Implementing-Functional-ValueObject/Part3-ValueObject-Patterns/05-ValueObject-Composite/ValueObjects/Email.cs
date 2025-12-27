using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using static LanguageExt.Prelude;

namespace ValueObjectComposite.ValueObjects;

/// <summary>
/// 5. 비교 불가능한 복합 값 객체 - ValueObject
/// 이메일 주소를 나타내는 값 객체 (여러 값 객체 조합)
/// 
/// 특징:
/// - 복잡한 검증 로직을 가진 값 객체
/// - 동등성 비교만 제공
/// - 여러 값 객체를 조합하여 더 복잡한 도메인 개념 표현
/// </summary>
public sealed class Email : ValueObject
{
    public EmailLocalPart LocalPart { get; }
    public EmailDomain Domain { get; }

    private Email(EmailLocalPart localPart, EmailDomain domain)
    {
        LocalPart = localPart;
        Domain = domain;
    }

    /// <summary>
    /// 이메일 값 객체 생성
    /// </summary>
    /// <param name="emailAddress">이메일 주소</param>
    /// <returns>성공 시 Email 값 객체, 실패 시 에러</returns>
    public static Fin<Email> Create(string emailAddress) =>
        CreateFromValidation(
            Validate(emailAddress),
            validValues => new Email(validValues.LocalPart, validValues.Domain));

    /// <summary>
    /// 이미 검증된 이메일로 값 객체 생성
    /// </summary>
    /// <param name="validatedValues">검증된 이메일 값들</param>
    /// <returns>Email 값 객체</returns>
    internal static Email CreateFromValidated((EmailLocalPart LocalPart, EmailDomain Domain) validatedValues) =>
        new Email(validatedValues.LocalPart, validatedValues.Domain);

    /// <summary>
    /// 이메일 주소 유효성 검증
    /// </summary>
    /// <param name="emailAddress">검증할 이메일 주소</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (EmailLocalPart LocalPart, EmailDomain Domain)> Validate(string emailAddress) =>
        from validEmail in ValidateEmailFormat(emailAddress)
        from validParts in ValidateEmailParts(validEmail)
        select validParts;

    /// <summary>
    /// 이메일 형식 검증
    /// </summary>
    /// <param name="email">검증할 이메일</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, string> ValidateEmailFormat(string email) =>
        !string.IsNullOrWhiteSpace(email) && email.Contains('@')
            ? email
            : DomainErrors.InvalidEmailFormat(email);

    /// <summary>
    /// 이메일 구성 요소 검증
    /// </summary>
    /// <param name="email">검증할 이메일</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, (EmailLocalPart LocalPart, EmailDomain Domain)> ValidateEmailParts(string email) =>
        from validParts in ValidateEmailSplit(email)
        from validLocalPart in ValidateLocalPart(validParts.localPart)
        from validDomain in ValidateDomain(validParts.domain)
        select (LocalPart: validLocalPart, Domain: validDomain);

    /// <summary>
    /// 이메일 분할 검증
    /// </summary>
    /// <param name="email">분할할 이메일</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, (string localPart, string domain)> ValidateEmailSplit(string email)
    {
        var parts = email.Split('@');
        return parts.Length == 2
            ? (localPart: parts[0], domain: parts[1])
            : DomainErrors.InvalidEmailFormat(email);
    }

    /// <summary>
    /// 로컬 부분 검증
    /// </summary>
    /// <param name="localPart">검증할 로컬 부분</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, EmailLocalPart> ValidateLocalPart(string localPart) =>
        EmailLocalPart
            .Validate(localPart)
            .Map(validLocalPart => EmailLocalPart.CreateFromValidated(validLocalPart));

    /// <summary>
    /// 도메인 검증
    /// </summary>
    /// <param name="domain">검증할 도메인</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, EmailDomain> ValidateDomain(string domain) =>
        EmailDomain
            .Validate(domain)
            .Map(validDomain => EmailDomain.CreateFromValidated(validDomain));

    /// <summary>
    /// 동등성 비교를 위한 구성 요소 반환
    /// </summary>
    /// <returns>동등성 비교 구성 요소</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return LocalPart;
        yield return Domain;
    }

    public override string ToString() => 
        $"{LocalPart}@{Domain}";

    internal static class DomainErrors
    {
        public static Error InvalidEmailFormat(string value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Email)}.{nameof(InvalidEmailFormat)}",
                errorCurrentValue: value,
                errorMessage: "");
    }
}
