using Functorium.Applications.Linq;
using static LanguageExt.Prelude;

namespace DesigningWithTypes.AggregateRoots.Contacts.Services;

/// <summary>
/// 이메일 고유성 검증 도메인 서비스
/// Specification 생성 → Repository DB 쿼리 → 결과 해석을 응집적으로 수행합니다.
/// </summary>
public sealed class ContactEmailCheckService : IDomainService
{
    private readonly IContactRepository _repository;

    public ContactEmailCheckService(IContactRepository repository)
        => _repository = repository;

    #region Error Types

    public sealed record EmailAlreadyInUse : DomainErrorKind.Custom;

    #endregion

    /// <summary>
    /// 이메일 고유성을 검증합니다.
    /// Specification 생성 → Repository DB 쿼리 → 결과 해석을 응집적으로 수행합니다.
    /// </summary>
    public FinT<IO, Unit> ValidateEmailUnique(
        EmailAddress email,
        Option<ContactId> excludeId = default)
    {
        var spec = new ContactEmailUniqueSpec(email, excludeId);
        return from exists in _repository.Exists(spec)
               from _ in CheckNotExists(email, exists)
               select unit;
    }

    private static Fin<Unit> CheckNotExists(EmailAddress email, bool exists)
    {
        if (exists)
            return DomainError.For<ContactEmailCheckService>(
                new EmailAlreadyInUse(),
                (string)email,
                "Email is already in use by another contact");

        return unit;
    }
}
