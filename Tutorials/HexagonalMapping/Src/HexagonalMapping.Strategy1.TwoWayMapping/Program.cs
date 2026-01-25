using HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Persistence;
using HexagonalMapping.Strategy1.TwoWayMapping.Adapters.Rest;
using HexagonalMapping.Strategy1.TwoWayMapping.Application;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== Strategy 1: Two-Way Mapping (권장) ===");
Console.WriteLine();
Console.WriteLine("이 전략은 Core와 Adapter에 각각 별도의 모델을 정의하고,");
Console.WriteLine("양방향 매퍼를 통해 변환합니다.");
Console.WriteLine();

// Setup: In-Memory Database
DbContextOptions<ProductDbContext> options = new DbContextOptionsBuilder<ProductDbContext>()
    .UseInMemoryDatabase("TwoWayMappingDemo")
    .Options;

await using ProductDbContext context = new(options);

// DI 구성 (수동)
// - Driven Adapter (출력 포트 구현): Repository
// - Use Case (입력 포트 구현): ProductService
// - Driving Adapter: Controller
ProductRepository repository = new(context);       // Driven Adapter
ProductService productService = new(repository);   // Use Case (Application)
ProductController controller = new(productService); // Driving Adapter

// Demo: Create Product
Console.WriteLine("[1] 상품 생성 (Use Case: CreateProduct)");
CreateProductRequest createRequest = new()
{
    Name = "Clean Architecture Book",
    Price = 45.99m,
    Currency = "USD"
};

ProductDto created = await controller.CreateAsync(createRequest);
Console.WriteLine($"    생성됨: {created.Name} - {created.FormattedPrice}");
Console.WriteLine($"    ID: {created.Id}");
Console.WriteLine();

// Demo: Get All Products
Console.WriteLine("[2] 전체 상품 조회 (Use Case: GetAllProducts)");
IReadOnlyList<ProductDto> products = await controller.GetAllAsync();
foreach (ProductDto product in products)
{
    Console.WriteLine($"    - {product.Name}: {product.FormattedPrice}");
}
Console.WriteLine();

// Demo: Update Product
Console.WriteLine("[3] 상품 수정 (Use Case: UpdateProductPrice)");
UpdateProductRequest updateRequest = new()
{
    Price = 39.99m,
    Currency = "USD"
};
ProductDto? updated = await controller.UpdateAsync(created.Id, updateRequest);
Console.WriteLine($"    수정됨: {updated!.Name} - {updated.FormattedPrice}");
Console.WriteLine();

// Demo: Hexagonal Architecture Flow
Console.WriteLine("[4] Hexagonal Architecture 전체 흐름");
Console.WriteLine(@"
    +-----------------------------------------------------------------------+
    |                     Driving Adapter (REST)                            |
    |  +---------------------------------------------------------------+   |
    |  | ProductController                                              |   |
    |  |   - HTTP Request/Response handling                             |   |
    |  |   - DTO <-> Domain mapping (ProductDtoMapper)                  |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | calls
                                       v
    +-----------------------------------------------------------------------+
    |                        Input Port                                     |
    |  +---------------------------------------------------------------+   |
    |  | IProductService                                                |   |
    |  |   - CreateProductAsync()                                       |   |
    |  |   - GetProductAsync()                                          |   |
    |  |   - UpdateProductPriceAsync()                                  |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | implements
                                       v
    +-----------------------------------------------------------------------+
    |                    Application (Use Case)                             |
    |  +---------------------------------------------------------------+   |
    |  | ProductService : IProductService                               |   |
    |  |   - Orchestrates Domain entities                               |   |
    |  |   - Coordinates business flow                                  |   |
    |  |   - Calls Output Port (IProductRepository)                     |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | calls
                                       v
    +-----------------------------------------------------------------------+
    |                       Domain Core                                     |
    |  +------------------+  +------------------+  +------------------+     |
    |  | Product          |  | ProductId        |  | Money            |     |
    |  | (Entity)         |  | (Strongly-typed) |  | (Value Object)   |     |
    |  +------------------+  +------------------+  +------------------+     |
    |  +---------------------------------------------------------------+   |
    |  | IProductRepository (Output Port)                               |   |
    |  |   - GetByIdAsync(), AddAsync(), UpdateAsync()                  |   |
    |  +---------------------------------------------------------------+   |
    +----------------------------------+------------------------------------+
                                       | implements
                                       v
    +-----------------------------------------------------------------------+
    |                     Driven Adapter (Persistence)                      |
    |  +---------------------------------------------------------------+   |
    |  | ProductRepository : IProductRepository                         |   |
    |  |   - Domain <-> Entity mapping (ProductMapper)                  |   |
    |  |   - DB access via EF Core                                      |   |
    |  +---------------------------------------------------------------+   |
    |  +---------------------------------------------------------------+   |
    |  | ProductEntity (Adapter-specific model)                         |   |
    |  |   - [Table], [Column] tech annotations                         |   |
    |  +---------------------------------------------------------------+   |
    +-----------------------------------------------------------------------+
");

Console.WriteLine("[5] Two-Way Mapping 포인트");
Console.WriteLine(@"
    Mapping Locations:
    1. REST Adapter: DTO <-> Domain (ProductDtoMapper)
       - CreateProductRequest -> Use Case parameters
       - Product -> ProductDto

    2. Persistence Adapter: Domain <-> Entity (ProductMapper)
       - Product -> ProductEntity (save)
       - ProductEntity -> Product (load)
");

Console.WriteLine("[6] 장점");
Console.WriteLine("    ✅ 명확한 아키텍처 경계 유지");
Console.WriteLine("    ✅ Core가 기술 의존성으로부터 완전히 자유로움");
Console.WriteLine("    ✅ 각 계층이 독립적으로 진화 가능");
Console.WriteLine("    ✅ Use Case가 명시적으로 정의됨");
Console.WriteLine();

Console.WriteLine("[7] 단점");
Console.WriteLine("    ⚠️ 더 많은 코드와 유지보수 필요");
Console.WriteLine("    ⚠️ 매핑 로직 오버헤드");
Console.WriteLine();

Console.WriteLine("=== Demo Complete ===");
