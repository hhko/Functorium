using Functorium.Adapters.SourceGenerators.Generators.EntityIdGenerator;
using Functorium.Testing.Actions.SourceGenerators;

using static Functorium.Tests.Unit.Abstractions.Constants.Constants;

namespace Functorium.Tests.Unit.AdaptersTests.SourceGenerators;

// # EntityIdGenerator 소스 생성기 테스트
//
// [GenerateEntityId] 속성이 붙은 Entity 클래스에 대해
// EntityId, EntityIdComparer, EntityIdConverter가 올바르게 생성되는지 검증합니다.
//
// ## 테스트 시나리오
//
// ### 1. 기본 생성 테스트
// - GenerateEntityIdAttribute 자동 생성 확인
//
// ### 2. EntityId 생성 테스트
// - 단순 Entity 클래스에 대한 EntityId 생성
// - 깊은 네임스페이스에서의 EntityId 생성
//
// ### 3. EF Core 지원 테스트
// - EntityIdComparer 생성 확인
// - EntityIdConverter 생성 확인
//
// ### 4. 직렬화 지원 테스트
// - JsonConverter 내장 확인
// - TypeConverter 내장 확인
//
[Trait(nameof(UnitTest), UnitTest.Functorium_Adapters_SourceGenerator)]
public sealed class EntityIdGeneratorTests
{
    private readonly EntityIdGenerator _sut;

    public EntityIdGeneratorTests()
    {
        _sut = new EntityIdGenerator();
    }

    #region 1. 기본 생성 테스트

    /// <summary>
    /// 시나리오: 소스 생성기가 [GenerateEntityId] Attribute를 자동으로 생성하는지 확인합니다.
    /// 이 Attribute는 Entity 클래스에 붙여 EntityId 생성을 지시하는 마커입니다.
    /// </summary>
    [Fact]
    public Task EntityIdGenerator_ShouldGenerate_GenerateEntityIdAttribute()
    {
        // Arrange
        string input = string.Empty;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");
    }

    #endregion

    #region 2. EntityId 생성 테스트

    /// <summary>
    /// 시나리오: 단순 Entity 클래스
    /// [GenerateEntityId] 속성이 붙은 Entity 클래스에 대해 EntityId가 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task EntityIdGenerator_ShouldGenerate_EntityId_ForSimpleEntity()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace MyApp.Domain.Entities;

            [GenerateEntityId]
            public class Product
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");
    }

    /// <summary>
    /// 시나리오: 깊은 네임스페이스
    /// 깊은 네임스페이스에서도 EntityId가 올바르게 생성되는지 확인합니다.
    /// </summary>
    [Fact]
    public Task EntityIdGenerator_ShouldGenerate_EntityId_WithDeepNamespace()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace Company.Project.Domain.Entities.Products;

            [GenerateEntityId]
            public class ProductCategory
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        return Verify(actual).UseDirectory("Snapshots/EntityIdGenerator");
    }

    #endregion

    #region 3. EF Core 지원 테스트

    /// <summary>
    /// 시나리오: EntityIdComparer 생성
    /// 생성된 코드에 EF Core ValueComparer가 포함되어 있는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_ShouldGenerate_EntityIdComparer()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Order
            {
                public string Description { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public sealed class OrderIdComparer : global::Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<OrderId>");
        actual.ShouldContain("(id1, id2) => id1.Value == id2.Value");
        actual.ShouldContain("id => id.Value.GetHashCode()");
    }

    /// <summary>
    /// 시나리오: EntityIdConverter 생성
    /// 생성된 코드에 EF Core ValueConverter가 포함되어 있는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_ShouldGenerate_EntityIdConverter()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Customer
            {
                public string Name { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public sealed class CustomerIdConverter : global::Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<CustomerId, string>");
        actual.ShouldContain("id => id.Value.ToString()");
        actual.ShouldContain("str => CustomerId.Create(str)");
    }

    #endregion

    #region 4. 직렬화 지원 테스트

    /// <summary>
    /// 시나리오: JsonConverter 내장
    /// 생성된 EntityId에 JsonConverter가 내장되어 있는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_GeneratedCode_ShouldContain_JsonConverter()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Invoice
            {
                public decimal Amount { get; set; }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("[JsonConverter(typeof(InvoiceIdJsonConverter))]");
        actual.ShouldContain("internal sealed class InvoiceIdJsonConverter : global::System.Text.Json.Serialization.JsonConverter<InvoiceId>");
        actual.ShouldContain("public override InvoiceId Read(ref Utf8JsonReader reader");
        actual.ShouldContain("public override void Write(Utf8JsonWriter writer");
    }

    /// <summary>
    /// 시나리오: TypeConverter 내장
    /// 생성된 EntityId에 TypeConverter가 내장되어 있는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_GeneratedCode_ShouldContain_TypeConverter()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Shipment
            {
                public string Destination { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("[TypeConverter(typeof(ShipmentIdTypeConverter))]");
        actual.ShouldContain("internal sealed class ShipmentIdTypeConverter : global::System.ComponentModel.TypeConverter");
        actual.ShouldContain("public override bool CanConvertFrom");
        actual.ShouldContain("public override bool CanConvertTo");
        actual.ShouldContain("public override object? ConvertFrom");
        actual.ShouldContain("public override object? ConvertTo");
    }

    #endregion

    #region 5. IEntityId 인터페이스 구현 테스트

    /// <summary>
    /// 시나리오: IEntityId 인터페이스 구현
    /// 생성된 EntityId가 IEntityId 인터페이스를 구현하는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_GeneratedCode_ShouldImplement_IEntityId()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Warehouse
            {
                public string Location { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("global::Functorium.Domains.Entities.IEntityId<WarehouseId>");
    }

    /// <summary>
    /// 시나리오: IParsable 인터페이스 구현
    /// 생성된 EntityId가 IParsable 인터페이스를 구현하는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_GeneratedCode_ShouldImplement_IParsable()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Inventory
            {
                public int Quantity { get; set; }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("global::System.IParsable<InventoryId>");
        actual.ShouldContain("public static InventoryId Parse(string s, global::System.IFormatProvider? provider)");
        actual.ShouldContain("public static bool TryParse(string? s, global::System.IFormatProvider? provider, out InventoryId result)");
    }

    #endregion

    #region 6. 팩토리 메서드 테스트

    /// <summary>
    /// 시나리오: 팩토리 메서드 생성
    /// 생성된 EntityId에 New(), Create(Ulid), Create(string) 팩토리 메서드가 포함되어 있는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_GeneratedCode_ShouldContain_FactoryMethods()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Payment
            {
                public decimal Amount { get; set; }
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public static PaymentId New() => new(global::System.Ulid.NewUlid())");
        actual.ShouldContain("public static PaymentId Create(global::System.Ulid id) => new(id)");
        actual.ShouldContain("public static PaymentId Create(string id)");
    }

    #endregion

    #region 7. 비교 연산자 테스트

    /// <summary>
    /// 시나리오: 비교 연산자 생성
    /// 생성된 EntityId에 비교 연산자가 포함되어 있는지 확인합니다.
    /// </summary>
    [Fact]
    public void EntityIdGenerator_GeneratedCode_ShouldContain_ComparisonOperators()
    {
        // Arrange
        string input = """
            using Functorium.Domains.SourceGenerators;

            namespace TestNamespace;

            [GenerateEntityId]
            public class Transaction
            {
                public string Status { get; set; } = string.Empty;
            }
            """;

        // Act
        string? actual = _sut.Generate(input);

        // Assert
        actual.ShouldNotBeNull();
        actual.ShouldContain("public int CompareTo(TransactionId other) => Value.CompareTo(other.Value)");
        actual.ShouldContain("public static bool operator <(TransactionId left, TransactionId right)");
        actual.ShouldContain("public static bool operator >(TransactionId left, TransactionId right)");
        actual.ShouldContain("public static bool operator <=(TransactionId left, TransactionId right)");
        actual.ShouldContain("public static bool operator >=(TransactionId left, TransactionId right)");
    }

    #endregion
}
