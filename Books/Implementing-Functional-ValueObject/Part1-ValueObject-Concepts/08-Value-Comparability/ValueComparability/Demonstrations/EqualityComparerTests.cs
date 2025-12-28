using LanguageExt;
using ValueComparability.Comparers;
using ValueComparability.ValueObjects;

namespace ValueComparability.Demonstrations;

/// <summary>
/// IEqualityComparer<T> 사용 예제 테스트
/// </summary>
public static class EqualityComparerTests
{
    /// <summary>
    /// EmailAddress를 안전하게 생성하는 헬퍼 메서드
    /// </summary>
    private static EmailAddress CreateEmailAddress(string value)
    {
        var result = from email in EmailAddress.Create(value)
                     select email;

        return result.Match(
            Succ: x => x,
            Fail: _ => EmailAddress.Create("default@example.com").Match(
                Succ: x => x,
                Fail: _ => throw new InvalidOperationException($"Failed to create EmailAddress with value {value}")
            )
        );
    }

    /// <summary>
    /// 기본 IEqualityComparer<T> 테스트
    /// </summary>
    public static void DemonstrateBasicEqualityComparer()
    {
        Console.WriteLine("=== 기본 IEqualityComparer<T> 테스트 ===");

        var result = from email1 in EmailAddress.Create("user@example.com")
                     from email2 in EmailAddress.Create("user@example.com")
                     from email3 in EmailAddress.Create("admin@example.com")
                     select (email1, email2, email3);

        result.Match(
            Succ: values =>
            {
                var (email1, email2, email3) = values;
                var comparer = new EmailAddressComparer();

                Console.WriteLine($"email1 = {email1}");
                Console.WriteLine($"email2 = {email2}");
                Console.WriteLine($"email3 = {email3}");
                Console.WriteLine();

                // 기본 비교 테스트
                Console.WriteLine("기본 비교 테스트:");
                Console.WriteLine($"comparer.Equals(email1, email2) = {comparer.Equals(email1, email2)}"); // true
                Console.WriteLine($"comparer.Equals(email1, email3) = {comparer.Equals(email1, email3)}"); // false
                Console.WriteLine($"comparer.Equals(email1, null) = {comparer.Equals(email1, null)}"); // false
                Console.WriteLine($"comparer.Equals(null, null) = {comparer.Equals(null, null)}"); // true
                Console.WriteLine();

                // 해시 코드 테스트
                Console.WriteLine("해시 코드 테스트:");
                Console.WriteLine($"email1.GetHashCode() = {email1!.GetHashCode()}");
                Console.WriteLine($"email2.GetHashCode() = {email2.GetHashCode()}");
                Console.WriteLine($"email3.GetHashCode() = {email3.GetHashCode()}");
                Console.WriteLine($"같은 값의 해시 코드가 같은가? {email1.GetHashCode() == email2.GetHashCode()}");
                Console.WriteLine();
            },
            Fail: error => Console.WriteLine($"생성 실패: {error.Message}")
        );
    }

    /// <summary>
    /// 컬렉션에서 IEqualityComparer<T> 사용 테스트
    /// </summary>
    public static void DemonstrateCollectionWithEqualityComparer()
    {
        Console.WriteLine("=== 컬렉션에서 IEqualityComparer<T> 사용 테스트 ===");

        List<EmailAddress> emails = [
            CreateEmailAddress("user1@example.com"),
            CreateEmailAddress("user2@example.com"),
            CreateEmailAddress("user1@example.com"), // 중복
            CreateEmailAddress("admin@example.com"),
            CreateEmailAddress("user2@example.com"), // 중복
            CreateEmailAddress("test@example.com")
        ];

        Console.WriteLine("원본 이메일 리스트:");
        foreach (var email in emails)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();

        // 기본 Distinct 사용 (IEquatable<T> 활용)
        var uniqueEmails = emails.Distinct().ToList();
        Console.WriteLine("기본 Distinct 후 (중복 제거):");
        foreach (var email in uniqueEmails)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();

        // HashSet 사용
        var emailSet = new System.Collections.Generic.HashSet<EmailAddress>(emails);
        Console.WriteLine("HashSet 사용 후 (중복 제거):");
        foreach (var email in emailSet)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();

        // 커스텀 EqualityComparer 사용
        var customComparer = new EmailAddressComparer();
        var uniqueWithComparer = emails.Distinct(customComparer).ToList();
        Console.WriteLine("커스텀 EqualityComparer 사용 후:");
        foreach (var email in uniqueWithComparer)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// 대소문자 무시 비교자 테스트
    /// </summary>
    public static void DemonstrateCaseInsensitiveComparer()
    {
        Console.WriteLine("=== 대소문자 무시 비교자 테스트 ===");

        List<EmailAddress> emails = [
            CreateEmailAddress("User@Example.com"),
            CreateEmailAddress("user@example.com"),
            CreateEmailAddress("ADMIN@EXAMPLE.COM"),
            CreateEmailAddress("admin@example.com"),
            CreateEmailAddress("Test@Example.Com"),
            CreateEmailAddress("test@example.com")
        ];

        Console.WriteLine("원본 이메일 리스트 (대소문자 혼재):");
        foreach (var email in emails)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();

        // 기본 비교자 사용 (대소문자 구분)
        var caseSensitiveComparer = new EmailAddressComparer();
        var uniqueCaseSensitive = emails.Distinct(caseSensitiveComparer).ToList();
        Console.WriteLine("대소문자 구분 비교자 사용 후:");
        foreach (var email in uniqueCaseSensitive)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();

        // 대소문자 무시 비교자 사용
        var caseInsensitiveComparer = new EmailAddressCaseInsensitiveComparer();
        var uniqueCaseInsensitive = emails.Distinct(caseInsensitiveComparer).ToList();
        Console.WriteLine("대소문자 무시 비교자 사용 후:");
        foreach (var email in uniqueCaseInsensitive)
        {
            Console.Write($"{email} ");
        }
        Console.WriteLine();
        Console.WriteLine();
    }

    /// <summary>
    /// Dictionary에서 IEqualityComparer<T> 사용 테스트
    /// </summary>
    public static void DemonstrateDictionaryWithEqualityComparer()
    {
        Console.WriteLine("=== Dictionary에서 IEqualityComparer<T> 사용 테스트 ===");

        List<(EmailAddress Email, string Name)> emailData = [
            (CreateEmailAddress("user1@example.com"), "User One"),
            (CreateEmailAddress("user2@example.com"), "User Two"),
            (CreateEmailAddress("user1@example.com"), "User One Updated"), // 중복 키
            (CreateEmailAddress("admin@example.com"), "Admin")
        ];

        // 기본 Dictionary 사용
        var basicDict = new Dictionary<EmailAddress, string>();
        foreach (var (email, name) in emailData)
        {
            if (!basicDict.ContainsKey(email))
            {
                basicDict[email] = name;
            }
        }

        Console.WriteLine("기본 Dictionary 결과:");
        foreach (var kvp in basicDict)
        {
            Console.WriteLine($"  {kvp.Key} -> {kvp.Value}");
        }

        // 커스텀 EqualityComparer 사용
        var customComparer = new EmailAddressComparer();
        var customDict = new Dictionary<EmailAddress, string>(customComparer);
        foreach (var (email, name) in emailData)
        {
            if (!customDict.ContainsKey(email))
            {
                customDict[email] = name;
            }
        }

        Console.WriteLine("커스텀 EqualityComparer 사용 Dictionary 결과:");
        foreach (var kvp in customDict)
        {
            Console.WriteLine($"  {kvp.Key} -> {kvp.Value}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// 성능 비교 테스트
    /// </summary>
    public static void DemonstratePerformanceComparison()
    {
        Console.WriteLine("=== 성능 비교 테스트 ===");

        var emails = Enumerable.Range(1, 10000)
            .Select(i => CreateEmailAddress($"user{i}@example.com"))
            .ToList();

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 기본 Distinct 성능
        stopwatch.Restart();
        var uniqueBasic = emails.Distinct().ToList();
        stopwatch.Stop();
        Console.WriteLine($"기본 Distinct 성능: {stopwatch.ElapsedMilliseconds}ms");

        // 커스텀 EqualityComparer 성능
        stopwatch.Restart();
        var customComparer = new EmailAddressComparer();
        var uniqueCustom = emails.Distinct(customComparer).ToList();
        stopwatch.Stop();
        Console.WriteLine($"커스텀 EqualityComparer 성능: {stopwatch.ElapsedMilliseconds}ms");

        // HashSet 성능
        stopwatch.Restart();
        var emailSet = new System.Collections.Generic.HashSet<EmailAddress>(emails);
        stopwatch.Stop();
        Console.WriteLine($"HashSet 성능: {stopwatch.ElapsedMilliseconds}ms");

        Console.WriteLine($"결과 개수: {uniqueBasic.Count} (기본), {uniqueCustom.Count} (커스텀), {emailSet.Count} (HashSet)");
        Console.WriteLine();
    }
}
