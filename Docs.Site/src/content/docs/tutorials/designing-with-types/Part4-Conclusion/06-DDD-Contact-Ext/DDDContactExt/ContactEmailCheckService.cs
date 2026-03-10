using static LanguageExt.Prelude;

namespace DDDContactExt;

/// <summary>
/// 이메일 고유성 검증 도메인 서비스
/// Application Layer에서 기존 Contact 목록을 조회 후 호출합니다.
/// </summary>
public sealed class ContactEmailCheckService : IDomainService
{
    #region Error Types

    public sealed record EmailAlreadyInUse : DomainErrorType.Custom;

    #endregion

    /// <summary>
    /// 이메일이 다른 Contact에서 사용 중인지 검증합니다.
    /// </summary>
    public Fin<Unit> ValidateEmailUnique(
        EmailAddress email,
        Seq<Contact> existingContacts,
        Option<ContactId> excludeId = default)
    {
        var isDuplicate = existingContacts
            .Filter(c => excludeId.Match(id => c.Id != id, () => true))
            .Any(c => c.EmailValue == (string)email);

        if (isDuplicate)
            return DomainError.For<ContactEmailCheckService>(
                new EmailAlreadyInUse(),
                (string)email,
                "Email is already in use by another contact");

        return unit;
    }
}
