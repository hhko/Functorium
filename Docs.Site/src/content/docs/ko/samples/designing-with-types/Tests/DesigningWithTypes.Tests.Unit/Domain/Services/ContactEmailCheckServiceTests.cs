using Functorium.Domains.Repositories;
using Functorium.Domains.Specifications;
using Functorium.Testing.Arrangements.Effects;

namespace DesigningWithTypes.Tests.Unit;

/// <summary>
/// ContactEmailCheckService 도메인 서비스 테스트
/// </summary>
[Trait("Sample", "DesigningWithTypes")]
public class ContactEmailCheckServiceTests
{
    [Fact]
    public async Task ValidateEmailUnique_ReturnsSuccess_WhenNotExists()
    {
        // Arrange
        var sut = CreateSut(existsResult: false);
        var email = EmailAddress.Create("test@example.com").ThrowIfFail();

        // Act
        var actual = await sut.ValidateEmailUnique(email).Run().RunAsync();

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateEmailUnique_ReturnsFail_WhenExists()
    {
        // Arrange
        var sut = CreateSut(existsResult: true);
        var email = EmailAddress.Create("test@example.com").ThrowIfFail();

        // Act
        var actual = await sut.ValidateEmailUnique(email).Run().RunAsync();

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    private static ContactEmailCheckService CreateSut(bool existsResult) =>
        new(new StubContactRepository(existsResult));

    private sealed class StubContactRepository(bool existsResult) : IContactRepository
    {
        public string RequestCategory => "Test";

        public FinT<IO, bool> Exists(Specification<Contact> spec) =>
            FinTFactory.Succ(existsResult);

        public FinT<IO, Contact> Create(Contact aggregate) => throw new NotImplementedException();
        public FinT<IO, Contact> GetById(ContactId id) => throw new NotImplementedException();
        public FinT<IO, Contact> Update(Contact aggregate) => throw new NotImplementedException();
        public FinT<IO, int> Delete(ContactId id) => throw new NotImplementedException();
        public FinT<IO, int> CreateRange(IReadOnlyList<Contact> aggregates) => throw new NotImplementedException();
        public FinT<IO, Seq<Contact>> GetByIds(IReadOnlyList<ContactId> ids) => throw new NotImplementedException();
        public FinT<IO, int> UpdateRange(IReadOnlyList<Contact> aggregates) => throw new NotImplementedException();
        public FinT<IO, int> DeleteRange(IReadOnlyList<ContactId> ids) => throw new NotImplementedException();
        public FinT<IO, int> Count(Specification<Contact> spec) => throw new NotImplementedException();
        public FinT<IO, int> DeleteBy(Specification<Contact> spec) => throw new NotImplementedException();
    }
}
