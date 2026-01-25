using HexagonalMapping.Strategy2.OneWayMapping.Adapters.Persistence;
using HexagonalMapping.Strategy2.OneWayMapping.Application;
using HexagonalMapping.Strategy2.OneWayMapping.Domain;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== Strategy 2: One-Way Mapping ===");
Console.WriteLine();
Console.WriteLine("이 전략은 공통 인터페이스를 정의하고,");
Console.WriteLine("Domain 엔티티와 Adapter 모델 모두 이를 구현합니다.");
Console.WriteLine();
Console.WriteLine("핵심 (문서 원문):");
Console.WriteLine("  \"Only one translation direction is needed - from core to adapter.\"");
Console.WriteLine("  (하나의 변환 방향만 필요 - Core에서 Adapter로)");
Console.WriteLine();

// Setup: In-Memory Database
DbContextOptions<ProductDbContext> options = new DbContextOptionsBuilder<ProductDbContext>()
    .UseInMemoryDatabase("OneWayMappingDemo")
    .Options;

await using ProductDbContext context = new(options);

// DI 구성 (수동)
ProductRepository repository = new(context);       // Driven Adapter
ProductService productService = new(repository);   // Use Case (Application)

// Demo: Create Product
Console.WriteLine("[1] 상품 생성 (Use Case: CreateProduct)");
Console.WriteLine("    Domain Product 생성 → IProductModel로 Repository에 전달");
Product product = await productService.CreateProductAsync("Domain-Driven Design Book", 54.99m, "USD");
Console.WriteLine($"    생성됨: {product.Name} - {product.FormattedPrice}");
Console.WriteLine($"    ID: {product.Id}");
Console.WriteLine();

// Demo: Get All Products - Returns IProductModel (not Product!)
Console.WriteLine("[2] 전체 상품 조회 (One-Way: IProductModel 직접 반환)");
Console.WriteLine("    ✅ Repository가 ProductEntity를 IProductModel로 직접 반환");
Console.WriteLine("    ✅ 변환 없음!");
IReadOnlyList<IProductModel> products = await productService.GetAllProductsAsync();
foreach (IProductModel p in products)
{
    Console.WriteLine($"    - {p.Name}: {p.Price} {p.Currency}");
    // 주의: p.FormattedPrice 사용 불가 - IProductModel에 없음!
}
Console.WriteLine();

// Demo: Get Product - Returns IProductModel
Console.WriteLine("[3] 상품 조회 (One-Way: IProductModel 반환)");
IProductModel? loaded = await productService.GetProductAsync(product.Id);
Console.WriteLine($"    조회됨 (IProductModel): {loaded!.Name} - {loaded.Price} {loaded.Currency}");
Console.WriteLine("    ⚠️ 비즈니스 메서드(FormattedPrice) 사용 불가");
Console.WriteLine();

// Demo: Update Product - Shows the overhead
Console.WriteLine("[4] 상품 가격 수정 (One-Way의 오버헤드 발생!)");
Console.WriteLine("    ⚠️ 비즈니스 로직(UpdatePrice)이 필요하므로 Product로 변환 필요");
Product? updated = await productService.UpdateProductPriceAsync(product.Id, 49.99m);
Console.WriteLine($"    수정됨 (Product): {updated!.Name} - {updated.FormattedPrice}");
Console.WriteLine("    ✅ 변환 후에는 비즈니스 메서드 사용 가능");
Console.WriteLine();

// Demo: Architecture Flow
Console.WriteLine("[5] One-Way Mapping 핵심 포인트");
Console.WriteLine(@"
    문서 원문:
    ""The adapter returns its own model since it implements the core's interface.""
    (Adapter는 Core의 인터페이스를 구현하므로 자신의 모델을 직접 반환)

    Mapping Direction:
    1. Domain -> Adapter (Save):
       Product implements IProductModel
       -> ProductEntity.FromModel(IProductModel) 변환 필요
       -> ONE-WAY: 이 방향만 변환 필요!

    2. Adapter -> Domain (Load):
       ProductEntity implements IProductModel
       -> IProductModel로 직접 반환 (변환 없음!)
       -> 비즈니스 로직 필요 시에만 Product.FromModel() 호출
");

// Demo: Show the interface limitation
Console.WriteLine("[6] One-Way Mapping의 제한사항");
Console.WriteLine(@"
    문서 원문:
    ""The interface must expose only data access methods, excluding business logic.""
    (인터페이스는 데이터 접근 메서드만 노출, 비즈니스 로직 제외)

    IProductModel interface:
    +----------------------------+
    | Guid Id { get; }           |  <- Data accessor only
    | string Name { get; }       |  <- Data accessor only
    | decimal Price { get; }     |  <- Data accessor only
    | string Currency { get; }   |  <- Data accessor only
    +----------------------------+
    | FormattedPrice?            |  <- NOT included! (business logic)
    | UpdatePrice()?             |  <- NOT included! (business logic)
    +----------------------------+

    결과:
    - 조회 시 비즈니스 메서드 사용 불가
    - 비즈니스 로직 필요 시 Product로 변환 필요
    - 이것이 '더 많은 오버헤드'의 원인
");

Console.WriteLine("[7] 저자 평가");
Console.WriteLine("    문서 원문:");
Console.WriteLine("    \"I don't like this strategy because it is less intuitive and,");
Console.WriteLine("     in my experience, is more overhead.\"");
Console.WriteLine();
Console.WriteLine("    번역:");
Console.WriteLine("    \"이 전략은 덜 직관적이고, 제 경험상 오히려");
Console.WriteLine("     더 많은 오버헤드가 발생하기 때문에 선호하지 않습니다.\"");
Console.WriteLine("    - Sven Woltmann");
Console.WriteLine();

Console.WriteLine("=== Demo Complete ===");
