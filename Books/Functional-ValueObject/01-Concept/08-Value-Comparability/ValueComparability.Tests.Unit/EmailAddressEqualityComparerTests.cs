using ValueComparability.Comparers;
using ValueComparability.ValueObjects;

namespace ValueComparability.Tests.Unit;

/// <summary>
/// EmailAddress 값 객체의 IEqualityComparer<T> 구현 테스트
/// 
/// 테스트 목적:
/// 1. 기본 EmailAddressComparer 동작 검증
/// 2. 대소문자 무시 EmailAddressCaseInsensitiveComparer 동작 검증
/// 3. 컬렉션에서의 중복 제거 기능 검증
/// 4. Dictionary에서의 키 비교 기능 검증
/// </summary>
[Trait("Concept-08-Value-Comparability", "EmailAddressEqualityComparerTests")]
public class EmailAddressEqualityComparerTests
{
    // 테스트 시나리오: 기본 비교자가 동일한 이메일을 올바르게 비교해야 한다
    [Fact]
    public void BasicComparer_ShouldReturnTrue_WhenComparingIdenticalEmails()
    {
        // Arrange
        var result1 = EmailAddress.Create("user@example.com");
        var result2 = EmailAddress.Create("user@example.com");
        var comparer = new EmailAddressComparer();

        // Act
        var actual = from email1 in result1
                     from email2 in result2
                     select comparer.Equals(email1, email2);

        // Assert
        actual.Match(
            Succ: result => result.ShouldBeTrue(),
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 기본 비교자가 다른 이메일을 올바르게 구분해야 한다
    [Fact]
    public void BasicComparer_ShouldReturnFalse_WhenComparingDifferentEmails()
    {
        // Arrange
        var result1 = EmailAddress.Create("user@example.com");
        var result2 = EmailAddress.Create("admin@example.com");
        var comparer = new EmailAddressComparer();

        // Act
        var actual = from email1 in result1
                     from email2 in result2
                     select comparer.Equals(email1, email2);

        // Assert
        actual.Match(
            Succ: result => result.ShouldBeFalse(),
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 기본 비교자가 null을 올바르게 처리해야 한다
    [Theory]
    [InlineData("user@example.com", null, false)]
    [InlineData(null, "user@example.com", false)]
    [InlineData(null, null, true)]
    public void BasicComparer_ShouldHandleNullCorrectly_WhenComparingWithNull(string? email1, string? email2, bool expected)
    {
        // Arrange
        var result1 = email1 != null ? EmailAddress.Create(email1) : Fin.Fail<EmailAddress>(Error.New("null"));
        var result2 = email2 != null ? EmailAddress.Create(email2) : Fin.Fail<EmailAddress>(Error.New("null"));
        var comparer = new EmailAddressComparer();

        // Act & Assert
        if (email1 != null && email2 != null)
        {
            var actual = from e1 in result1
                         from e2 in result2
                         select comparer.Equals(e1, e2);

            actual.Match(
                Succ: result => result.ShouldBe(expected),
                Fail: error => throw new Exception($"생성 실패: {error.Message}")
            );
        }
        else
        {
            // null 케이스는 직접 테스트
            EmailAddress? e1 = email1 != null ? EmailAddress.Create(email1).Match(Succ: x => x, Fail: _ => null) : null;
            EmailAddress? e2 = email2 != null ? EmailAddress.Create(email2).Match(Succ: x => x, Fail: _ => null) : null;
            var result = comparer.Equals(e1, e2);
            result.ShouldBe(expected);
        }
    }

    // 테스트 시나리오: 대소문자 무시 비교자가 대소문자 차이를 무시해야 한다
    [Theory]
    [InlineData("User@Example.com", "user@example.com", true)]
    [InlineData("ADMIN@EXAMPLE.COM", "admin@example.com", true)]
    [InlineData("Test@Example.Com", "test@example.com", true)]
    [InlineData("user@example.com", "admin@example.com", false)]
    public void CaseInsensitiveComparer_ShouldIgnoreCase_WhenComparingEmails(string email1, string email2, bool expected)
    {
        // Arrange
        var result1 = EmailAddress.Create(email1);
        var result2 = EmailAddress.Create(email2);
        var comparer = new EmailAddressCaseInsensitiveComparer();

        // Act
        var actual = from e1 in result1
                     from e2 in result2
                     select comparer.Equals(e1, e2);

        // Assert
        actual.Match(
            Succ: result => result.ShouldBe(expected),
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 기본 비교자로 Distinct를 사용할 때 EmailAddress의 정규화로 인해 대소문자가 구분되지 않는다
    [Fact]
    public void Distinct_ShouldNotDistinguishCase_WhenUsingBasicComparer()
    {
        // Arrange
        var emails = new[] {
            "User@Example.com",     // 1
            "user@example.com",
            "ADMIN@EXAMPLE.COM",    // 2
            "admin@example.com"
        };
        var emailAddresses = emails.Select(e => EmailAddress.Create(e).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                   .ToList();
        var comparer = new EmailAddressComparer();
        var expectedCount = 2; // EmailAddress가 ToLowerInvariant()로 정규화되므로 2개만 남음

        // Act
        var actual = emailAddresses.Distinct(comparer).ToList();

        // Assert
        actual.Count.ShouldBe(expectedCount);
    }

    // 테스트 시나리오: 대소문자 무시 비교자로 Distinct를 사용할 때 대소문자를 무시해야 한다
    [Fact]
    public void Distinct_ShouldIgnoreCase_WhenUsingCaseInsensitiveComparer()
    {
        // Arrange
        var emails = new[] { "User@Example.com", "user@example.com", "ADMIN@EXAMPLE.COM", "admin@example.com" };
        var emailAddresses = emails.Select(e => EmailAddress.Create(e).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                   .ToList();
        var comparer = new EmailAddressCaseInsensitiveComparer();
        var expectedCount = 2; // User@example.com과 admin@example.com만 남음

        // Act
        var actual = emailAddresses.Distinct(comparer).ToList();

        // Assert
        actual.Count.ShouldBe(expectedCount);
    }

    // 테스트 시나리오: HashSet에서 기본 비교자가 올바르게 동작해야 한다
    [Fact]
    public void HashSet_ShouldWorkCorrectly_WhenUsingBasicComparer()
    {
        // Arrange
        var emails = new[] {
            "user1@example.com",        // 1
            "user2@example.com",        // 2
            "user1@example.com",
            "admin@example.com"         // 3
        };
        var emailAddresses = emails.Select(e => EmailAddress.Create(e).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패")))
                                   .ToList();
        var comparer = new EmailAddressComparer();
        var expectedCount = 3; // 중복 제거 후 3개

        // Act
        var actual = new System.Collections.Generic.HashSet<EmailAddress>(emailAddresses, comparer);

        // Assert
        actual.Count.ShouldBe(expectedCount);
    }

    // 테스트 시나리오: Dictionary에서 기본 비교자가 올바르게 동작해야 한다
    [Fact]
    public void Dictionary_ShouldWorkCorrectly_WhenUsingBasicComparer()
    {
        // Arrange
        var emailData = new[]
        {
            ("user1@example.com", "User One"),
            ("user2@example.com", "User Two"),
            ("user1@example.com", "User One Updated"), // 중복 키
            ("admin@example.com", "Admin")
        };
        var comparer = new EmailAddressComparer();
        var expectedCount = 3; // 중복 제거 후 3개

        // Act
        var dictionary = new Dictionary<EmailAddress, string>(comparer);
        foreach (var (emailStr, name) in emailData)
        {
            var email = EmailAddress.Create(emailStr).Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패"));
            if (!dictionary.ContainsKey(email))
            {
                dictionary[email] = name;
            }
        }

        // Assert
        dictionary.Count.ShouldBe(expectedCount);
        dictionary[EmailAddress.Create("user1@example.com").Match(Succ: x => x, Fail: _ => throw new Exception("생성 실패"))].ShouldBe("User One");
    }

    // 테스트 시나리오: GetHashCode가 일관된 해시 코드를 반환해야 한다
    [Fact]
    public void GetHashCode_ShouldReturnConsistentHashCode_WhenUsingSameEmail()
    {
        // Arrange
        var result1 = EmailAddress.Create("user@example.com");
        var result2 = EmailAddress.Create("user@example.com");
        var comparer = new EmailAddressComparer();

        // Act
        var actual = from email1 in result1
                     from email2 in result2
                     select (comparer.GetHashCode(email1), comparer.GetHashCode(email2));

        // Assert
        actual.Match(
            Succ: hashCodes =>
            {
                var (hash1, hash2) = hashCodes;
                hash1.ShouldBe(hash2);
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 대소문자 무시 비교자의 GetHashCode가 대소문자를 무시한 해시를 반환해야 한다
    [Fact]
    public void CaseInsensitiveGetHashCode_ShouldReturnSameHashCode_WhenUsingDifferentCase()
    {
        // Arrange
        var result1 = EmailAddress.Create("User@Example.com");
        var result2 = EmailAddress.Create("user@example.com");
        var comparer = new EmailAddressCaseInsensitiveComparer();

        // Act
        var actual = from email1 in result1
                     from email2 in result2
                     select (comparer.GetHashCode(email1), comparer.GetHashCode(email2));

        // Assert
        actual.Match(
            Succ: hashCodes =>
            {
                var (hash1, hash2) = hashCodes;
                hash1.ShouldBe(hash2);
            },
            Fail: error => throw new Exception($"생성 실패: {error.Message}")
        );
    }
}
