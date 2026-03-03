using ValueObjectFramework.ValueObjects.ComparableNot.PrimitiveValueObjects;

namespace ValueObjectFramework.Tests.Unit.ValueObjectTests.ComparableNot.PrimitiveValueObjects;

/// <summary>
/// BinaryData 값 객체 테스트
/// SimpleValueObject<byte[]> 기반으로 비교 기능 없이 구현
/// 
/// 테스트 목적:
/// 1. 바이너리 데이터 생성 및 검증 검증
/// 2. LINQ Expression을 활용한 함수형 체이닝 검증
/// 3. 동등성 비교 기능 검증
/// </summary>
[Trait("Concept-11-ValueObject-Framework", "BinaryDataTests")]
public class BinaryDataTests
{
    // 테스트 시나리오: 유효한 바이너리 데이터로 BinaryData 인스턴스를 생성할 수 있어야 한다
    [Fact]
    public void Create_ShouldReturnSuccessResult_WhenValidBinaryData()
    {
        // Arrange
        var binaryData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"

        // Act
        var actual = BinaryData.Create(binaryData);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.IfSucc(data => data.ToString().ShouldContain("5 bytes"));
    }

    // 테스트 시나리오: 빈 배열로 BinaryData 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenEmptyArray()
    {
        // Arrange
        var emptyArray = new byte[0];

        // Act
        var actual = BinaryData.Create(emptyArray);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("바이너리 데이터는 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: null 배열로 BinaryData 생성 시 실패해야 한다
    [Fact]
    public void Create_ShouldReturnFailureResult_WhenNullArray()
    {
        // Arrange
        byte[]? nullArray = null;

        // Act
        var actual = BinaryData.Create(nullArray!);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.IfFail(error => error.Message.ShouldBe("바이너리 데이터는 비어있을 수 없습니다"));
    }

    // 테스트 시나리오: BinaryData 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Fact]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenSameContent()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 1, 2, 3 };
        var binaryData1 = BinaryData.Create(data1).IfFail(_ => throw new Exception("생성 실패"));
        var binaryData2 = BinaryData.Create(data2).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = binaryData1.Equals(binaryData2);

        // Assert
        actual.ShouldBeTrue();
        binaryData1.GetHashCode().ShouldBe(binaryData2.GetHashCode());
    }

    // 테스트 시나리오: BinaryData 인스턴스들이 올바르게 동등성을 비교해야 한다
    [Fact]
    public void Equals_ShouldReturnCorrectEqualityResult_WhenDifferentContent()
    {
        // Arrange
        var data1 = new byte[] { 1, 2, 3 };
        var data2 = new byte[] { 4, 5, 6 };
        var binaryData1 = BinaryData.Create(data1).IfFail(_ => throw new Exception("생성 실패"));
        var binaryData2 = BinaryData.Create(data2).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = binaryData1.Equals(binaryData2);

        // Assert
        actual.ShouldBeFalse();
    }

    // 테스트 시나리오: ToString 메서드가 올바른 형식으로 바이너리 데이터 정보를 반환해야 한다
    [Fact]
    public void ToString_ShouldReturnFormattedBinaryDataInfo_WhenCalled()
    {
        // Arrange
        var data = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F }; // "Hello"
        var binaryData = BinaryData.Create(data).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = binaryData.ToString();

        // Assert
        actual.ShouldContain("BinaryData[5 bytes:");
        actual.ShouldContain("48 65 6C 6C 6F");
    }

    // 테스트 시나리오: ToString 메서드가 원본 데이터를 포함해야 한다
    [Fact]
    public void ToString_ShouldContainOriginalData_WhenCalled()
    {
        // Arrange
        var originalData = new byte[] { 1, 2, 3, 4, 5 };
        var binaryData = BinaryData.Create(originalData).IfFail(_ => throw new Exception("생성 실패"));

        // Act
        var actual = binaryData.ToString();

        // Assert
        actual.ShouldContain("5 bytes");
        actual.ShouldContain("01 02 03 04 05");
    }
}
