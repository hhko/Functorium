using LanguageExt;
using LanguageExt.Common;
using SimpleValueObject.ValueObjects;

namespace SimpleValueObject;

/// <summary>
/// 1. 비교 불가능한 primitive 값 객체 데모 - SimpleValueObject<T>
/// 
/// 이 데모는 SimpleValueObject<T>의 특징을 보여줍니다:
/// - 기본적인 동등성 비교와 해시코드 제공
/// - 비교 연산자는 지원하지 않음 (IComparable<T> 미구현)
/// - 명시적 타입 변환 지원
/// - 단순한 값 래핑에 적합
/// </summary>
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 1. 비교 불가능한 primitive 값 객체 - SimpleValueObject<T> ===");
        Console.WriteLine("부모 클래스: SimpleValueObject<byte[]>");
        Console.WriteLine("예시: BinaryData (이진 데이터)");
        Console.WriteLine();

        Console.WriteLine("📋 특징:");
        Console.WriteLine("   ✅ 기본적인 동등성 비교와 해시코드 제공");
        Console.WriteLine("   ❌ 비교 연산자는 지원하지 않음 (IComparable<T> 미구현)");
        Console.WriteLine("   ✅ 명시적 타입 변환 지원");
        Console.WriteLine("   ✅ 단순한 값 래핑에 적합");
        Console.WriteLine();

        // 성공 케이스
        Console.WriteLine("🔍 성공 케이스:");
        var data1 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }); // "Hello"
        var data2 = BinaryData.Create(new byte[] { 0x57, 0x6F, 0x72, 0x6C, 0x64 }); // "World"
        var data3 = BinaryData.Create(new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }); // "Hello" (동일)

        if (data1.IsSucc)
        {
            Console.WriteLine($"   ✅ BinaryData(Hello): {(BinaryData)data1}");
        }
        if (data2.IsSucc)
        {
            Console.WriteLine($"   ✅ BinaryData(World): {(BinaryData)data2}");
        }
        if (data3.IsSucc)
        {
            Console.WriteLine($"   ✅ BinaryData(Hello): {(BinaryData)data3}");
        }
        Console.WriteLine();

        // 동등성 비교
        Console.WriteLine("📊 동등성 비교:");
        if (data1.IsSucc && data2.IsSucc)
        {
            Console.WriteLine($"   {(BinaryData)data1} == {(BinaryData)data2} = {(BinaryData)data1 == (BinaryData)data2}");
        }
        if (data1.IsSucc && data3.IsSucc)
        {
            Console.WriteLine($"   {(BinaryData)data1} == {(BinaryData)data3} = {(BinaryData)data1 == (BinaryData)data3}");
        }
        Console.WriteLine();

        // 타입 변환
        Console.WriteLine("🔄 타입 변환:");
        if (data1.IsSucc)
        {
            var binaryData = (BinaryData)data1;
            var bytes = (byte[])binaryData;
            Console.WriteLine($"   (byte[]){binaryData} = [{string.Join(", ", bytes.Select(b => $"0x{b:X2}"))}]");
        }
        Console.WriteLine();

        // 해시코드
        Console.WriteLine("🔢 해시코드:");
        if (data1.IsSucc && data3.IsSucc)
        {
            var binaryData1 = (BinaryData)data1;
            var binaryData3 = (BinaryData)data3;
            Console.WriteLine($"   {binaryData1}.GetHashCode() = {binaryData1.GetHashCode()}");
            Console.WriteLine($"   {binaryData3}.GetHashCode() = {binaryData3.GetHashCode()}");
            Console.WriteLine($"   동일한 값의 해시코드가 같은가? {binaryData1.GetHashCode() == binaryData3.GetHashCode()}");
        }
        Console.WriteLine();

        // 실패 케이스
        Console.WriteLine("❌ 실패 케이스:");
        var invalidData1 = BinaryData.Create(null!);
        var invalidData2 = BinaryData.Create(new byte[0]);

        if (invalidData1.IsFail)
        {
            Console.WriteLine($"   BinaryData(null): {(Error)invalidData1}");
        }
        if (invalidData2.IsFail)
        {
            Console.WriteLine($"   BinaryData(empty): {(Error)invalidData2}");
        }
        Console.WriteLine();

        // 비교 연산자 테스트 (컴파일 에러가 발생해야 함)
        Console.WriteLine("⚠️  비교 연산자 테스트:");
        Console.WriteLine("   SimpleValueObject<T>는 IComparable<T>를 구현하지 않으므로");
        Console.WriteLine("   <, <=, >, >= 연산자를 사용할 수 없습니다.");
        Console.WriteLine("   이는 의도된 설계입니다.");
        Console.WriteLine();

        // 컬렉션에서의 사용
        Console.WriteLine("📦 컬렉션에서의 사용:");
        var binaryDataList = new List<BinaryData>();
        var testData = new[]
        {
            new byte[] { 0x41, 0x42, 0x43 }, // "ABC"
            new byte[] { 0x44, 0x45, 0x46 }, // "DEF"
            new byte[] { 0x47, 0x48, 0x49 }  // "GHI"
        };

        foreach (var bytes in testData)
        {
            var binaryData = BinaryData.Create(bytes);
            if (binaryData.IsSucc)
            {
                binaryDataList.Add((BinaryData)binaryData);
            }
        }

        Console.WriteLine("   BinaryData 목록:");
        foreach (var binaryData in binaryDataList)
        {
            Console.WriteLine($"     {binaryData}");
        }

        Console.WriteLine();
        Console.WriteLine("✅ 데모가 성공적으로 완료되었습니다!");
    }
}