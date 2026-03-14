namespace DesigningWithTypes.Contacts;

/// <summary>
/// Contact Aggregate Root 리포지토리 인터페이스
/// </summary>
public interface IContactRepository : IRepository<Contact, ContactId>
{
    FinT<IO, bool> Exists(Specification<Contact> spec);
}
