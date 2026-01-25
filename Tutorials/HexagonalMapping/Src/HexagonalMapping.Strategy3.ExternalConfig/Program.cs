Console.WriteLine("=== Strategy 3: External Configuration (외부 설정) ===");
Console.WriteLine();
Console.WriteLine("이 전략은 매핑 메타데이터를 코드 어노테이션 대신");
Console.WriteLine("설정 파일(XML) 또는 Fluent API에 저장합니다.");
Console.WriteLine();

// 주의: 이 전략은 실제 구현에서 제한사항이 있습니다.
// Domain 엔티티를 직접 사용하려면 EF Core의 요구사항을 충족해야 합니다:
// - 파라미터 없는 생성자 (또는 생성자 바인딩 설정)
// - Value Object (Owned Entity) 처리의 복잡성

Console.WriteLine("[1] 개념");
Console.WriteLine("    Domain 클래스에 어노테이션을 추가하지 않고,");
Console.WriteLine("    외부 설정 파일이나 Fluent API에서 매핑을 정의합니다.");
Console.WriteLine();

// Demo: Hexagonal Architecture Flow
Console.WriteLine("[2] Hexagonal Architecture 전체 흐름");
Console.WriteLine(@"
    +-----------------------------------------------------------------------+
    |                     Driving Adapter (REST)                            |
    |  +---------------------------------------------------------------+   |
    |  | ProductController                                              |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | calls
                                       v
    +-----------------------------------------------------------------------+
    |                        Input Port                                     |
    |  +---------------------------------------------------------------+   |
    |  | IProductService                                                |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | implements
                                       v
    +-----------------------------------------------------------------------+
    |                    Application (Use Case)                             |
    |  +---------------------------------------------------------------+   |
    |  | ProductService : IProductService                               |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | calls
                                       v
    +-----------------------------------------------------------------------+
    |                       Domain Core                                     |
    |  +---------------------------------------------------------------+   |
    |  | Product (NO tech annotations!)                                 |   |
    |  | ProductId, Money (Value Objects)                               |   |
    |  +---------------------------------------------------------------+   |
    |  +---------------------------------------------------------------+   |
    |  | IProductRepository (Output Port)                               |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | implements
                                       v
    +-----------------------------------------------------------------------+
    |                     Driven Adapter (Persistence)                      |
    |  +---------------------------------------------------------------+   |
    |  | ProductRepository : IProductRepository                         |   |
    |  |   - Uses Domain entities directly                              |   |
    |  |   - NO MAPPING CODE needed                                     |   |
    |  +---------------------------------------------------------------+   |
    |  +---------------------------------------------------------------+   |
    |  | ProductDbContext                                               |   |
    |  |   - Fluent API or XML defines ORM mapping                      |   |
    |  |   - Mapping config is external to Domain                       |   |
    |  +---------------------------------------------------------------+   |
    +-----------------------------------------------------------------------+
");

// Demo: XML Mapping Example
Console.WriteLine("[3] XML 매핑 예시 (NHibernate 스타일)");
Console.WriteLine(@"
    <?xml version=""1.0"" encoding=""utf-8""?>
    <hibernate-mapping>
      <class name=""Product"" table=""products"">
        <id name=""Id"" column=""id"">
          <generator class=""assigned""/>
        </id>
        <property name=""Name"" column=""name"" length=""200""/>
        <component name=""Price"">
          <property name=""Amount"" column=""price""/>
          <property name=""Currency"" column=""currency""/>
        </component>
      </class>
    </hibernate-mapping>
");

// Demo: Fluent API Example (EF Core 방식)
Console.WriteLine("[4] Fluent API 매핑 (EF Core 방식)");
Console.WriteLine(@"
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable(""products"");
            entity.HasKey(""Id"");
            entity.Property(e => e.Name)
                  .HasColumnName(""name"")
                  .HasMaxLength(200);
            entity.OwnsOne(e => e.Price, money =>
            {
                money.Property(m => m.Amount).HasColumnName(""price"");
                money.Property(m => m.Currency).HasColumnName(""currency"");
            });
        });
    }
");

Console.WriteLine("[5] External Configuration의 Mapping 특징");
Console.WriteLine(@"
    Mapping Location:
    1. Domain -> Adapter (Save):
       - NO explicit mapping code!
       - Domain entity used directly
       - ORM reads mapping from XML or Fluent API

    2. Adapter -> Domain (Load):
       - NO explicit mapping code!
       - ORM reconstructs Domain entity directly
       - Mapping config defines how to bind data
");

Console.WriteLine("[6] 실제 구현의 제한사항");
Console.WriteLine(@"
    ⚠️ EF Core는 Domain 엔티티를 직접 사용할 때 다음을 요구합니다:
       - 파라미터 없는 생성자 (또는 생성자 바인딩 설정)
       - Value Object (Owned Entity) 처리의 복잡성
       - 이는 Domain 모델의 순수성을 해칠 수 있습니다

    ⚠️ 결과적으로 External Configuration만으로는 불충분하며,
       Domain 모델이 ORM의 제약을 일부 수용해야 합니다.
");

Console.WriteLine("[7] 장단점");
Console.WriteLine("    장점:");
Console.WriteLine("    ✅ Domain 클래스에 기술 어노테이션 없음");
Console.WriteLine("    ✅ 코드 중복 없음 (별도 Adapter 모델 불필요)");
Console.WriteLine("    ✅ 매핑 코드 불필요");
Console.WriteLine();
Console.WriteLine("    단점:");
Console.WriteLine("    ⚠️ 매핑이 코드와 분리되어 혼란 야기");
Console.WriteLine("    ⚠️ IDE 지원 부족 (리팩토링 시 깨지기 쉬움)");
Console.WriteLine("    ⚠️ Domain 모델이 ORM의 제약을 받음");
Console.WriteLine("    ⚠️ 어노테이션 기반 접근법보다 덜 선호됨");
Console.WriteLine();

Console.WriteLine("[8] 저자 평가");
Console.WriteLine("    \"코드 중복을 피할 수 있지만, 혼란스럽다(confusing).\"");
Console.WriteLine("    - Sven Woltmann");
Console.WriteLine();

Console.WriteLine("[9] 결론");
Console.WriteLine("    이 전략은 이론적으로는 매력적이지만,");
Console.WriteLine("    실제 구현에서는 많은 제약이 있습니다.");
Console.WriteLine("    Two-Way Mapping (전략 1)이 더 실용적입니다.");
Console.WriteLine();

Console.WriteLine("=== Demo Complete ===");
