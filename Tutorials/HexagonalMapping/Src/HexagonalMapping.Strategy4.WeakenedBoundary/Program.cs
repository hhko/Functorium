using HexagonalMapping.Strategy4.WeakenedBoundary.Adapters.Persistence;
using HexagonalMapping.Strategy4.WeakenedBoundary.Application;
using HexagonalMapping.Strategy4.WeakenedBoundary.Domain;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== Strategy 4: Weakened Boundaries (약화된 경계) ===");
Console.WriteLine("    ❌ Anti-pattern - 이 방식은 권장되지 않습니다!");
Console.WriteLine();
Console.WriteLine("이 전략은 Domain Core에서 ORM 라이브러리로의");
Console.WriteLine("의존성을 허용합니다. 도메인 엔티티에 기술 어노테이션을");
Console.WriteLine("직접 배치하여 아키텍처 격리를 포기합니다.");
Console.WriteLine();

// Setup: In-Memory Database
DbContextOptions<ProductDbContext> options = new DbContextOptionsBuilder<ProductDbContext>()
    .UseInMemoryDatabase("WeakenedBoundaryDemo")
    .Options;

await using ProductDbContext context = new(options);

// DI 구성 (수동)
// - Driven Adapter (출력 포트 구현): Repository
// - Use Case (입력 포트 구현): ProductService
ProductRepository repository = new(context);       // Driven Adapter
ProductService productService = new(repository);   // Use Case (Application)

// Demo: Create Product via Use Case
Console.WriteLine("[1] 상품 생성 (Use Case: CreateProduct)");
Product product = await productService.CreateProductAsync("The Pragmatic Programmer", 49.99m, "USD");
Console.WriteLine($"    Product: {product.Name} - {product.FormattedPrice}");
Console.WriteLine();

// Demo: Show the anti-pattern
Console.WriteLine("[2] Anti-pattern 코드 예시");
Console.WriteLine(@"
    // BAD: Domain class with tech annotations
    [Table(""products"")]
    public class Product
    {
        [Key]
        [Column(""id"")]
        public Guid Id { get; private set; }

        [Required]
        [MaxLength(200)]
        [Column(""product_name"")]
        public string Name { get; private set; }

        [Column(""price"")]
        public decimal Price { get; private set; }

        // Private ctor for EF Core (another tech requirement)
        private Product() { }
    }
");

// Demo: Save and Load via Use Case
Console.WriteLine("[3] 저장 및 조회 (Use Case 사용)");
Product? loaded = await productService.GetProductAsync(product.Id);
Console.WriteLine($"    조회됨: {loaded!.Name} - {loaded.FormattedPrice}");
Console.WriteLine("    하지만 이 간편함에는 큰 대가가 따릅니다...");
Console.WriteLine();

// Demo: Update via Use Case
Console.WriteLine("[4] 상품 가격 수정 (Use Case: UpdateProductPrice)");
Product? updated = await productService.UpdateProductPriceAsync(product.Id, 39.99m);
Console.WriteLine($"    수정됨: {updated!.Name} - {updated.FormattedPrice}");
Console.WriteLine();

// Demo: Hexagonal Architecture Flow (with problems)
Console.WriteLine("[5] Hexagonal Architecture 흐름 (문제점 포함)");
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
    |                       Domain Core  *** CONTAMINATED ***               |
    |  +---------------------------------------------------------------+   |
    |  | Product                                                        |   |
    |  |   [Table], [Column], [Key] <-- TECH ANNOTATIONS IN DOMAIN!     |   |
    |  |   private Product() {}    <-- EF Core requirement              |   |
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
    |  |   - NO MAPPING NEEDED (Domain = Entity)                        |   |
    |  |   - But Domain is now polluted!                                |   |
    |  +---------------------------------------------------------------+   |
    +-----------------------------------------------------------------------+
");

// Demo: Problems
Console.WriteLine("[6] 문제점: 깨진 창문 이론 (Broken Windows Theory)");
Console.WriteLine(@"
    1. First violation: ""It's just annotations...""
       -> [Table], [Column] added

    2. Second violation: ""EF Core requires it...""
       -> Private parameterless ctor added
       -> Virtual navigation properties added

    3. Third violation: ""JSON serialization needs it...""
       -> [JsonIgnore], [JsonPropertyName] added

    4. Fourth violation: ""Validation is convenient here...""
       -> [Required], [Range], [StringLength] added

    5. Eventually...
       -> Domain class covered with dozens of annotations
       -> Complete mixing of business logic and tech concerns
       -> Untestable, hard to maintain
");

Console.WriteLine("[7] 의존성 방향의 위반");
Console.WriteLine(@"
    Correct dependency direction (Hexagonal Architecture):
    +---------------------------------------------+
    |           Adapter (Persistence)             |
    |                    |                        |
    |                    v depends on             |
    |              Domain Core                    |
    |           (No tech dependencies)            |
    +---------------------------------------------+

    Weakened Boundaries dependency direction:
    +---------------------------------------------+
    |           Adapter (Persistence)             |
    |                    |                        |
    |                    |                        |
    |              Domain Core --------------------+
    |                    |                        |
    |                    v depends on             |
    |         EF Core, System.ComponentModel      |
    |           (Tech dependencies!)              |
    +---------------------------------------------+
");

Console.WriteLine("[8] 저자 평가");
Console.WriteLine("    \"I would always advise against this option.\"");
Console.WriteLine("    (이 옵션은 항상 권장하지 않습니다.)");
Console.WriteLine("    - Sven Woltmann");
Console.WriteLine();

Console.WriteLine("[9] 결론");
Console.WriteLine("    ❌ 절대 이 방식을 사용하지 마세요.");
Console.WriteLine("    ✅ Two-Way Mapping (전략 1)을 사용하세요.");
Console.WriteLine();

Console.WriteLine("=== Demo Complete ===");
