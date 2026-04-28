// ---------------------------------------------------------
// Chapter 7: EF Core Repository
// ---------------------------------------------------------
// EfCoreRepositoryBase<TAggregate, TId, TModel>는 EF Core 기반의
// IRepository 구현체입니다.
//
// 핵심 개념:
//   1. Domain Model vs Persistence Model 분리
//   2. ToDomain/ToModel 매핑
//   3. PropertyMap을 통한 Specification Expression 변환
//   4. ReadQuery()의 AsNoTracking + Include 자동 적용
//
// 이 챕터는 실제 EF Core 없이 매핑 개념을 학습합니다.
// ---------------------------------------------------------

using EfCoreRepository;

Console.WriteLine("=== Chapter 7: EF Core Repository ===");
Console.WriteLine();

// 1. Domain Model 생성
var product = new Product(ProductId.New(), "Keyboard", 49_900m);
Console.WriteLine($"Domain: Id={product.Id}, Name={product.Name}, Price={product.Price:N0}");

// 2. Domain → Persistence Model 변환
var model = ProductMapper.ToModel(product);
Console.WriteLine($"Model:  Id={model.Id}, Name={model.Name}, Price={model.Price:N0}");

// 3. Persistence → Domain Model 복원
var restored = ProductMapper.ToDomain(model);
Console.WriteLine($"Restored: Id={restored.Id}, Name={restored.Name}, Price={restored.Price:N0}");

// 4. 동일성 확인 (ID 기반)
Console.WriteLine($"Same entity: {product == restored}");
Console.WriteLine();

// 5. EfCoreRepositoryBase 구조 설명
Console.WriteLine("EfCoreRepositoryBase requires:");
Console.WriteLine("  abstract DbContext DbContext      → EF Core context");
Console.WriteLine("  abstract DbSet<TModel> DbSet      → Entity set");
Console.WriteLine("  abstract ToDomain(TModel)          → Model → Domain");
Console.WriteLine("  abstract ToModel(TAggregate)       → Domain → Model");
Console.WriteLine();
Console.WriteLine("PropertyMap enables Specification translation:");
Console.WriteLine("  Domain Expression: p => p.Price > 10000");
Console.WriteLine("  → Model Expression: m => m.Price > 10000");
Console.WriteLine("  → SQL: WHERE Price > 10000");
Console.WriteLine();

Console.WriteLine("Done.");
