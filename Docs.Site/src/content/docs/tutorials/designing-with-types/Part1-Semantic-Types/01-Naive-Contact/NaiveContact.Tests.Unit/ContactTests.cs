namespace NaiveContact.Tests.Unit;

/// <summary>
/// 나이브 Contact의 타입 혼동 문제를 보여주는 테스트
///
/// 테스트 목적:
/// 1. string 타입은 firstName과 lastName을 구분하지 못함을 증명
/// 2. 잘못된 값이 컴파일되는 것을 시연
/// </summary>
[Trait("Part1-Semantic-Types", "01-NaiveContact")]
public class ContactTests
{
    [Fact]
    public void Create_AcceptsSwappedNames_BecauseAllStrings()
    {
        // Arrange — firstName과 lastName을 의도적으로 뒤바꿈
        string firstName = "HyungHo";
        string lastName = "Ko";

        // Act — 실수로 뒤바꿔 전달
        var contact = Contact.Create(lastName, firstName, "test@example.com");

        // Assert — 컴파일러는 이 버그를 잡지 못함
        contact.FirstName.ShouldBe(lastName); // "Ko"가 FirstName에 들어감
        contact.LastName.ShouldBe(firstName); // "HyungHo"가 LastName에 들어감
    }

    [Fact]
    public void Create_AcceptsEmptyEmail_BecauseNoValidation()
    {
        // Act — 빈 이메일도 허용됨
        var contact = Contact.Create("HyungHo", "Ko", "");

        // Assert — 빈 문자열이 그대로 저장됨
        contact.EmailAddress.ShouldBeEmpty();
    }

    [Fact]
    public void Create_AcceptsInvalidEmail_BecauseNoValidation()
    {
        // Act — 유효하지 않은 이메일도 허용됨
        var contact = Contact.Create("HyungHo", "Ko", "not-an-email");

        // Assert — 검증 없이 그대로 저장됨
        contact.EmailAddress.ShouldBe("not-an-email");
    }
}
