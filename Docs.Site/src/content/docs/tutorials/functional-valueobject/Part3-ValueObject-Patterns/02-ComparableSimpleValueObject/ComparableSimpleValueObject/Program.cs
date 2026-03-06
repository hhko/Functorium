using ComparableSimpleValueObject.ValueObjects;
using LanguageExt;
using LanguageExt.Common;

namespace ComparableSimpleValueObject;

/// <summary>
/// 2. 비교 가능한 primitive 값 객체 데모 - ComparableSimpleValueObject<T>
/// 
/// 이 데모는 ComparableSimpleValueObject<T>의 특징을 보여줍니다:
/// - 자동으로 IComparable<T> 구현
/// - 모든 비교 연산자 오버로딩 (<, <=, >, >=)
/// - 명시적 타입 변환 지원
/// - 동등성 비교와 해시코드 자동 제공
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 2. 비교 가능한 primitive 값 객체 - ComparableSimpleValueObject<T> ===");
        Console.WriteLine("부모 클래스: ComparableSimpleValueObject<int>");
        Console.WriteLine("예시: UserId (사용자 ID)");
        Console.WriteLine();

        Console.WriteLine("📋 특징:");
        Console.WriteLine("   ✅ 자동으로 IComparable<UserId> 구현");
        Console.WriteLine("   ✅ 모든 비교 연산자 오버로딩 (<, <=, >, >=)");
        Console.WriteLine("   ✅ 명시적 타입 변환 지원");
        Console.WriteLine("   ✅ 동등성 비교와 해시코드 자동 제공");
        Console.WriteLine();

        // 성공 케이스
        Console.WriteLine("🔍 성공 케이스:");
        var id1 = UserId.Create(123);
        var id2 = UserId.Create(456);
        var id3 = UserId.Create(123);

        if (id1.IsSucc)
        {
            Console.WriteLine($"   ✅ UserId(123): {(UserId)id1}");
        }
        if (id2.IsSucc)
        {
            Console.WriteLine($"   ✅ UserId(456): {(UserId)id2}");
        }
        if (id3.IsSucc)
        {
            Console.WriteLine($"   ✅ UserId(123): {(UserId)id3}");
        }
        Console.WriteLine();

        // 동등성 비교
        Console.WriteLine("📊 동등성 비교:");
        if (id1.IsSucc && id2.IsSucc)
        {
            Console.WriteLine($"   {(UserId)id1} == {(UserId)id2} = {(UserId)id1 == (UserId)id2}");
        }
        if (id1.IsSucc && id3.IsSucc)
        {
            Console.WriteLine($"   {(UserId)id1} == {(UserId)id3}  =  {(UserId)id1 == (UserId)id3}");
        }
        Console.WriteLine();

        // 비교 기능 (IComparable<T>)
        Console.WriteLine("📊 비교 기능 (IComparable<T>):");
        if (id1.IsSucc && id2.IsSucc)
        {
            var userId1 = (UserId)id1;
            var userId2 = (UserId)id2;
            Console.WriteLine($"   {userId1} < {userId2} = {userId1 < userId2}");
            Console.WriteLine($"   {userId1} <= {userId2} = {userId1 <= userId2}");
            Console.WriteLine($"   {userId1} > {userId2} = {userId1 > userId2}");
            Console.WriteLine($"   {userId1} >= {userId2} = {userId1 >= userId2}");
        }
        Console.WriteLine();

        // 타입 변환
        Console.WriteLine("🔄 타입 변환:");
        if (id1.IsSucc)
        {
            var userId = (UserId)id1;
            Console.WriteLine($"   (int){userId} = {(int)userId}");
        }
        Console.WriteLine();

        // 해시코드
        Console.WriteLine("🔢 해시코드:");
        if (id1.IsSucc && id3.IsSucc)
        {
            var userId1 = (UserId)id1;
            var userId3 = (UserId)id3;
            Console.WriteLine($"   {userId1}.GetHashCode() = {userId1.GetHashCode()}");
            Console.WriteLine($"   {userId3}.GetHashCode() = {userId3.GetHashCode()}");
            Console.WriteLine($"   동일한 값의 해시코드가 같은가? {userId1.GetHashCode() == userId3.GetHashCode()}");
        }
        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("❌ 실패 케이스:");
        var invalidId1 = UserId.Create(0);
        var invalidId2 = UserId.Create(-1);

        if (invalidId1.IsFail)
        {
            Console.WriteLine($"   UserId(0): {(Error)invalidId1}");
        }
        if (invalidId2.IsFail)
        {
            Console.WriteLine($"   UserId(-1): {(Error)invalidId2}");
        }
        Console.WriteLine();

        // 정렬 데모
        Console.WriteLine("📈 정렬 데모:");
        var userIds = new List<UserId>();
        var ids = new[] { 456, 123, 789, 234, 567 };
        
        foreach (var id in ids)
        {
            var userId = UserId.Create(id);
            if (userId.IsSucc)
            {
                userIds.Add((UserId)userId);
            }
        }

        userIds.Sort(); // IComparable<T> 덕분에 자동 정렬 가능

        Console.WriteLine("   정렬된 UserId 목록:");
        foreach (var userId in userIds)
        {
            Console.WriteLine($"     {userId}");
        }

        Console.WriteLine();
        Console.WriteLine("✅ 데모가 성공적으로 완료되었습니다!");
    }
}