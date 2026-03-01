using RepositorySpec;
using RepositorySpec.Specifications;

Console.WriteLine("=== Repository + Specification ===\n");

// --- Before: 메서드 폭발 문제 ---
Console.WriteLine("--- Before: 메서드 폭발 (Method Explosion) ---");
Console.WriteLine("  FindByCategory(string category)");
Console.WriteLine("  FindByPriceRange(decimal min, decimal max)");
Console.WriteLine("  FindInStock()");
Console.WriteLine("  FindByCategoryAndPriceRange(string category, decimal min, decimal max)");
Console.WriteLine("  FindInStockByCategory(string category)");
Console.WriteLine("  FindInStockByPriceRange(decimal min, decimal max)");
Console.WriteLine("  ... 조건이 늘어날수록 메서드가 기하급수적으로 증가!");

Console.WriteLine();

// --- After: Specification 패턴 ---
Console.WriteLine("--- After: Specification 패턴 ---");
Console.WriteLine("  FindAll(Specification<Product> spec)  // 단 하나의 메서드!");
Console.WriteLine("  Exists(Specification<Product> spec)   // 단 하나의 메서드!");

Console.WriteLine();

// --- 사용 예시 ---
Console.WriteLine("--- Specification 조합 예시 ---");

var inStock = new ProductInStockSpec();
var affordable = new ProductPriceRangeSpec(0, 10_000);
var electronics = new ProductCategorySpec("전자제품");

Console.WriteLine($"  재고 있는 상품: {inStock}");
Console.WriteLine($"  1만원 이하: {affordable}");
Console.WriteLine($"  전자제품: {electronics}");
Console.WriteLine($"  재고 있는 전자제품: {inStock & electronics}");
Console.WriteLine($"  재고 있고 1만원 이하: {inStock & affordable}");

Console.WriteLine();
Console.WriteLine("Repository는 WHAT(무엇을 찾을지)을 모르고,");
Console.WriteLine("Specification은 HOW(어디서 찾을지)를 모릅니다.");
Console.WriteLine("=> 관심사의 완전한 분리!");
