using DDDContact;

namespace DDDContact.Tests.Unit;

/// <summary>
/// PostalAddress 복합 VO 테스트
///
/// 테스트 목적:
/// 1. LINQ 모나딕 합성 Create 성공/실패
/// 2. CreateFromValidated 직접 생성
/// </summary>
[Trait("Part4-Conclusion", "04-DDDContact")]
public class PostalAddressTests
{
    [Fact]
    public void Create_ReturnsSuccess_WhenValid()
    {
        // Act
        var actual = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601");

        // Assert
        actual.IsSucc.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenZipInvalid()
    {
        // Act
        var actual = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "bad");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void Create_ReturnsFail_WhenStateInvalid()
    {
        // Act
        var actual = PostalAddress.Create("456 Oak Ave", "Chicago", "Illinois", "60601");

        // Assert
        actual.IsFail.ShouldBeTrue();
    }

    [Fact]
    public void CreateFromValidated_CreatesDirectly()
    {
        // Arrange
        var address1 = String50.CreateFromValidated("456 Oak Ave");
        var city = String50.CreateFromValidated("Chicago");
        var state = StateCode.CreateFromValidated("IL");
        var zip = ZipCode.CreateFromValidated("60601");

        // Act
        var actual = PostalAddress.CreateFromValidated(address1, city, state, zip);

        // Assert
        actual.Address1.ShouldBe(address1);
        actual.City.ShouldBe(city);
        actual.State.ShouldBe(state);
        actual.Zip.ShouldBe(zip);
    }
}
