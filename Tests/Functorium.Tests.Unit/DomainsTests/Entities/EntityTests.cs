using Functorium.Domains.Entities;
using LanguageExt;
using LanguageExt.Common;
using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.DomainsTests.Entities;

/// <summary>
/// 테스트용 EntityId
/// </summary>
public readonly struct TestEntityId : IEntityId<TestEntityId>
{
    public Ulid Value { get; }

    private TestEntityId(Ulid value) => Value = value;

    public static TestEntityId New() => new(Ulid.NewUlid());
    public static TestEntityId Create(Ulid id) => new(id);
    public static TestEntityId Create(string id) => new(Ulid.Parse(id));

    public static TestEntityId Parse(string s, IFormatProvider? provider) => new(Ulid.Parse(s));
    public static bool TryParse(string? s, IFormatProvider? provider, out TestEntityId result)
    {
        if (Ulid.TryParse(s, out var ulid))
        {
            result = new(ulid);
            return true;
        }
        result = default;
        return false;
    }

    public bool Equals(TestEntityId other) => Value.Equals(other.Value);
    public int CompareTo(TestEntityId other) => Value.CompareTo(other.Value);
    public override bool Equals(object? obj) => obj is TestEntityId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(TestEntityId left, TestEntityId right) => left.Equals(right);
    public static bool operator !=(TestEntityId left, TestEntityId right) => !left.Equals(right);
}

/// <summary>
/// 테스트용 Entity
/// </summary>
public sealed class TestEntity : Entity<TestEntityId>
{
    public string Name { get; private set; }

    private TestEntity(TestEntityId id, string name) : base(id)
    {
        Name = name;
    }

    public static TestEntity Create(TestEntityId id, string name) => new(id, name);

    public static Fin<TestEntity> CreateWithValidation(TestEntityId id, string name)
    {
        Validation<Error, string> validation = string.IsNullOrWhiteSpace(name)
            ? Error.New("Name cannot be empty")
            : name;

        return CreateFromValidation<TestEntity, string>(
            validation,
            n => new TestEntity(id, n));
    }
}

/// <summary>
/// 다른 타입의 테스트용 Entity
/// </summary>
public sealed class OtherTestEntity : Entity<TestEntityId>
{
    public string Value { get; private set; }

    private OtherTestEntity(TestEntityId id, string value) : base(id)
    {
        Value = value;
    }

    public static OtherTestEntity Create(TestEntityId id, string value) => new(id, value);
}

[Trait(nameof(UnitTest), UnitTest.Functorium_Domains)]
public class EntityTests
{
    #region Equality Tests

    [Fact]
    public void Equals_ReturnsTrue_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = TestEntity.Create(id, "Entity 2");

        // Act
        var actual = entity1.Equals(entity2);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentId()
    {
        // Arrange
        var entity1 = TestEntity.Create(TestEntityId.New(), "Entity 1");
        var entity2 = TestEntity.Create(TestEntityId.New(), "Entity 2");

        // Act
        var actual = entity1.Equals(entity2);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenDifferentType()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = OtherTestEntity.Create(id, "Other Entity");

        // Act
        var actual = entity1.Equals(entity2);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsFalse_WhenComparedWithNull()
    {
        // Arrange
        var entity = TestEntity.Create(TestEntityId.New(), "Entity");

        // Act
        var actual = entity.Equals(null);

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void Equals_ReturnsTrue_WhenSameReference()
    {
        // Arrange
        var entity = TestEntity.Create(TestEntityId.New(), "Entity");

        // Act
        var actual = entity.Equals(entity);

        // Assert
        actual.ShouldBeTrue();
    }

    #endregion

    #region Operator Tests

    [Fact]
    public void OperatorEquals_ReturnsTrue_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = TestEntity.Create(id, "Entity 2");

        // Act
        var actual = entity1 == entity2;

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_ReturnsFalse_WhenDifferentId()
    {
        // Arrange
        var entity1 = TestEntity.Create(TestEntityId.New(), "Entity 1");
        var entity2 = TestEntity.Create(TestEntityId.New(), "Entity 2");

        // Act
        var actual = entity1 == entity2;

        // Assert
        actual.ShouldBeFalse();
    }

    [Fact]
    public void OperatorEquals_ReturnsTrue_WhenBothNull()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;

        // Act
        var actual = entity1 == entity2;

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void OperatorEquals_ReturnsFalse_WhenOneIsNull()
    {
        // Arrange
        var entity1 = TestEntity.Create(TestEntityId.New(), "Entity");
        TestEntity? entity2 = null;

        // Act
        var actual1 = entity1 == entity2;
        var actual2 = entity2 == entity1;

        // Assert
        actual1.ShouldBeFalse();
        actual2.ShouldBeFalse();
    }

    [Fact]
    public void OperatorNotEquals_ReturnsTrue_WhenDifferentId()
    {
        // Arrange
        var entity1 = TestEntity.Create(TestEntityId.New(), "Entity 1");
        var entity2 = TestEntity.Create(TestEntityId.New(), "Entity 2");

        // Act
        var actual = entity1 != entity2;

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void OperatorNotEquals_ReturnsFalse_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = TestEntity.Create(id, "Entity 2");

        // Act
        var actual = entity1 != entity2;

        // Assert
        actual.ShouldBeFalse();
    }

    #endregion

    #region GetHashCode Tests

    [Fact]
    public void GetHashCode_ReturnsSameValue_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = TestEntity.Create(id, "Entity 2");

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.ShouldBe(hash2);
    }

    [Fact]
    public void GetHashCode_ReturnsDifferentValue_WhenDifferentId()
    {
        // Arrange
        var entity1 = TestEntity.Create(TestEntityId.New(), "Entity 1");
        var entity2 = TestEntity.Create(TestEntityId.New(), "Entity 2");

        // Act
        var hash1 = entity1.GetHashCode();
        var hash2 = entity2.GetHashCode();

        // Assert
        hash1.ShouldNotBe(hash2);
    }

    #endregion

    #region CreateFromValidation Tests

    [Fact]
    public void CreateFromValidation_ReturnsSuccess_WhenValidationSucceeds()
    {
        // Arrange
        var id = TestEntityId.New();
        var name = "Valid Name";

        // Act
        var actual = TestEntity.CreateWithValidation(id, name);

        // Assert
        actual.IsSucc.ShouldBeTrue();
        actual.Match(
            Succ: entity =>
            {
                entity.Id.ShouldBe(id);
                entity.Name.ShouldBe(name);
            },
            Fail: _ => Assert.Fail("Should succeed"));
    }

    [Fact]
    public void CreateFromValidation_ReturnsFail_WhenValidationFails()
    {
        // Arrange
        var id = TestEntityId.New();
        var name = "";

        // Act
        var actual = TestEntity.CreateWithValidation(id, name);

        // Assert
        actual.IsFail.ShouldBeTrue();
        actual.Match(
            Succ: _ => Assert.Fail("Should fail"),
            Fail: error => error.Message.ShouldBe("Name cannot be empty"));
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void HashSet_ContainsEntity_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = TestEntity.Create(id, "Entity 2");
        var set = new System.Collections.Generic.HashSet<TestEntity> { entity1 };

        // Act
        var actual = set.Contains(entity2);

        // Assert
        actual.ShouldBeTrue();
    }

    [Fact]
    public void Dictionary_RetrievesValue_WhenSameId()
    {
        // Arrange
        var id = TestEntityId.New();
        var entity1 = TestEntity.Create(id, "Entity 1");
        var entity2 = TestEntity.Create(id, "Entity 2");
        var dict = new Dictionary<TestEntity, string> { { entity1, "Value" } };

        // Act
        var actual = dict.TryGetValue(entity2, out var value);

        // Assert
        actual.ShouldBeTrue();
        value.ShouldBe("Value");
    }

    #endregion
}
