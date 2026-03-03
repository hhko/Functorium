using SimpleValueObject.ValueObjects;
using LanguageExt;

namespace SimpleValueObject.Tests.Unit;

/// <summary>
/// BinaryData 값 객체의 SimpleValueObject 패턴 테스트
///
/// 학습 목표:
/// 1. 비교 불가능한 primitive 값 객체 패턴 이해
/// 2. SimpleValueObject<T> 기반 값 객체 생성 검증
/// 3. 값 동등성 비교 동작 확인
/// </summary>
[Trait("Part3-Patterns", "01-SimpleValueObject")]
public class BinaryDataTests
{
    // 테스트 시나리오: 유효한 바이트 배열로 BinaryData 생성 성공
    [Fact]
    public void Create_ReturnsSuccess_WhenByteArrayIsNotEmpty()
    {
        // Arrange
        byte[] value = [0x01, 0x02, 0x03];

        // Act
        Fin<BinaryData> actual = BinaryData.Create(value);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: binaryData =>
            {
                // 명시적 변환 연산자를 통해 Value 접근
                ((byte[])binaryData).ShouldBe(value);
            },
            Fail: error => throw new Exception($"예상치 못한 실패: {error.Message}")
        );
    }

    // 테스트 시나리오: 빈 바이트 배열로 BinaryData 생성 실패
    [Fact]
    public void Create_ReturnsFail_WhenByteArrayIsEmpty()
    {
        // Arrange
        byte[] value = [];

        // Act
        Fin<BinaryData> actual = BinaryData.Create(value);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: null 바이트 배열로 BinaryData 생성 실패
    [Fact]
    public void Create_ReturnsFail_WhenByteArrayIsNull()
    {
        // Arrange
        byte[]? value = null;

        // Act
        Fin<BinaryData> actual = BinaryData.Create(value!);

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    // 테스트 시나리오: 동일한 바이트 배열 내용으로 생성된 두 BinaryData는 동등해야 함
    [Fact]
    public void Equals_ReturnsTrue_WhenBinaryDataHaveSameContent()
    {
        // Arrange
        byte[] value1 = [0x01, 0x02, 0x03];
        byte[] value2 = [0x01, 0x02, 0x03];
        var binaryData1 = BinaryData.Create(value1).Match(
            Succ: bd => bd,
            Fail: _ => throw new Exception("생성 실패"));
        var binaryData2 = BinaryData.Create(value2).Match(
            Succ: bd => bd,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert - 배열 내용이 같으면 동등함 (값 기반 비교)
        binaryData1.Equals(binaryData2).ShouldBeTrue();
    }

    // 테스트 시나리오: 다른 바이트 배열 내용으로 생성된 두 BinaryData는 동등하지 않아야 함
    [Fact]
    public void Equals_ReturnsFalse_WhenBinaryDataHaveDifferentContent()
    {
        // Arrange
        byte[] value1 = [0x01, 0x02, 0x03];
        byte[] value2 = [0x04, 0x05, 0x06];
        var binaryData1 = BinaryData.Create(value1).Match(
            Succ: bd => bd,
            Fail: _ => throw new Exception("생성 실패"));
        var binaryData2 = BinaryData.Create(value2).Match(
            Succ: bd => bd,
            Fail: _ => throw new Exception("생성 실패"));

        // Act & Assert
        binaryData1.Equals(binaryData2).ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드가 의미 있는 문자열 반환
    [Fact]
    public void ToString_ReturnsFormattedString_WhenBinaryDataIsValid()
    {
        // Arrange
        byte[] value = [0x01, 0x02, 0x03];
        var binaryData = BinaryData.Create(value).Match(
            Succ: bd => bd,
            Fail: _ => throw new Exception("생성 실패"));

        // Act
        string actual = binaryData.ToString();

        // Assert
        actual.ShouldContain("BinaryData");
        actual.ShouldContain("3 bytes");
    }

    // 테스트 시나리오: 순수 함수 동작 검증
    [Fact]
    public void Create_IsPureFunction_WhenCalledMultipleTimes()
    {
        // Arrange
        byte[] value = [0x01, 0x02, 0x03];

        // Act
        Fin<BinaryData> actual1 = BinaryData.Create(value);
        Fin<BinaryData> actual2 = BinaryData.Create(value);

        // Assert
        actual1.IsSucc.ShouldBeTrue();
        actual2.IsSucc.ShouldBeTrue();
    }

    // 테스트 시나리오: 참조 동작 확인 - 현재 구현은 원본 배열을 참조함
    // Note: 진정한 불변성을 원하면 생성자에서 배열을 복사해야 함
    [Fact]
    public void BinaryData_ReferencesOriginalArray_WhenNotCloned()
    {
        // Arrange
        byte[] value = [0x01, 0x02, 0x03];
        var binaryData = BinaryData.Create(value).Match(
            Succ: bd => bd,
            Fail: _ => throw new Exception("생성 실패"));

        // Act - 원본 배열 수정
        value[0] = 0xFF;

        // Assert - 현재 구현은 원본 배열을 참조하므로 변경이 반영됨
        // Note: SimpleValueObject<byte[]>는 배열을 복사하지 않음
        ((byte[])binaryData)[0].ShouldBe((byte)0xFF);
    }
}
