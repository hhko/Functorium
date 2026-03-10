using DDDContactExt;

namespace DDDContactExt.Tests.Unit;

/// <summary>
/// PostalAddress 복합 VO 테스트
/// </summary>
[Trait("Part4-Conclusion", "05-DDDContactExt")]
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

    #region ValueObject Equality

    [Fact]
    public void Equals_ReturnsTrue_WhenSameValues()
    {
        // Arrange
        var addr1 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();
        var addr2 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

        // Assert
        addr1.ShouldBe(addr2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentAddress()
    {
        // Arrange
        var addr1 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();
        var addr2 = PostalAddress.Create("789 Elm St", "Chicago", "IL", "60601").ThrowIfFail();

        // Assert
        addr1.ShouldNotBe(addr2);
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentZip()
    {
        // Arrange
        var addr1 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();
        var addr2 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60602").ThrowIfFail();

        // Assert
        addr1.ShouldNotBe(addr2);
    }

    [Fact]
    public void GetHashCode_ReturnsSame_WhenEqual()
    {
        // Arrange
        var addr1 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();
        var addr2 = PostalAddress.Create("456 Oak Ave", "Chicago", "IL", "60601").ThrowIfFail();

        // Assert
        addr1.GetHashCode().ShouldBe(addr2.GetHashCode());
    }

    #endregion
}
