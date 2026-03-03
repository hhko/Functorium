// ---------------------------------------------------------
// Chapter 5: Repository Interface
// ---------------------------------------------------------
// IRepository<TAggregate, TId>는 Aggregate Root 단위
// 영속화를 위한 공통 인터페이스입니다.
//
// 8개 CRUD 메서드:
//   단건: Create, GetById, Update, Delete
//   복수: CreateRange, GetByIds, UpdateRange, DeleteRange
//
// 반환 타입: FinT<IO, T>
//   - Fin<T>: 성공(Succ) 또는 실패(Fail) 결과
//   - IO: 부수 효과를 추적하는 모나드
//   - FinT<IO, T>: 두 모나드의 조합 (Monad Transformer)
//
// 제네릭 제약:
//   TAggregate : AggregateRoot<TId>  → Aggregate Root만 허용
//   TId : struct, IEntityId<TId>     → 값 타입 ID만 허용
// ---------------------------------------------------------

using RepositoryInterface;

Console.WriteLine("=== Chapter 5: Repository Interface ===");
Console.WriteLine();

// 1. ProductId 생성
var productId = ProductId.New();
Console.WriteLine($"ProductId: {productId}");

// 2. Product Aggregate 생성
var product = new Product(productId, "Keyboard", 49_900m);
Console.WriteLine($"Product: {product.Name}, Price: {product.Price:N0}");
Console.WriteLine();

// 3. IRepository<Product, ProductId>가 제공하는 8개 CRUD 메서드 설명
Console.WriteLine("IRepository<Product, ProductId> CRUD operations:");
Console.WriteLine("  [Single] Create(product)        → FinT<IO, Product>");
Console.WriteLine("  [Single] GetById(id)             → FinT<IO, Product>");
Console.WriteLine("  [Single] Update(product)         → FinT<IO, Product>");
Console.WriteLine("  [Single] Delete(id)              → FinT<IO, int>");
Console.WriteLine("  [Batch]  CreateRange(products)   → FinT<IO, Seq<Product>>");
Console.WriteLine("  [Batch]  GetByIds(ids)           → FinT<IO, Seq<Product>>");
Console.WriteLine("  [Batch]  UpdateRange(products)   → FinT<IO, Seq<Product>>");
Console.WriteLine("  [Batch]  DeleteRange(ids)        → FinT<IO, int>");
Console.WriteLine();

// 4. IProductRepository는 IRepository를 확장
Console.WriteLine("IProductRepository extends IRepository with:");
Console.WriteLine("  Exists(Specification<Product>)   → FinT<IO, bool>");
Console.WriteLine();

Console.WriteLine("Done.");
