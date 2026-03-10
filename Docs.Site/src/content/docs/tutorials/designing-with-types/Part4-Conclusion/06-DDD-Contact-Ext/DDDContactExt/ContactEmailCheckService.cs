using static LanguageExt.Prelude;

namespace DDDContactExt;

/// <summary>
/// 이메일 고유성 검증 도메인 서비스
/// Application Layer에서 기존 Contact 정보를 조회 후 호출합니다.
/// </summary>
public sealed class ContactEmailCheckService : IDomainService
{
    #region Error Types

    public sealed record EmailAlreadyInUse : DomainErrorType.Custom;

    #endregion

    /// <summary>
    /// 이메일이 다른 Contact에서 사용 중인지 검증합니다.
    /// Aggregate 전체가 아닌 필요한 최소 정보만 수신합니다.
    /// </summary>
    public Fin<Unit> ValidateEmailUnique(
        EmailAddress email,
        Seq<(ContactId Id, string? EmailValue)> existingContacts,
        Option<ContactId> excludeId = default)
    {
        var isDuplicate = existingContacts
            .Filter(c => excludeId.Match(id => c.Id != id, () => true))
            .Any(c => c.EmailValue == (string)email);

        if (isDuplicate)
            return DomainError.For<ContactEmailCheckService>(
                new EmailAlreadyInUse(),
                (string)email,
                "이미 다른 연락처에서 사용 중인 이메일입니다");

        return unit;
    }
}
