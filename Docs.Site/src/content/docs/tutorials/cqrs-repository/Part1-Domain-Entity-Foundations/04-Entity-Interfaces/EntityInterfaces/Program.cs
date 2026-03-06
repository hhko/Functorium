using EntityInterfaces;

Console.WriteLine("=== Entity Interfaces ===\n");

// 1. 상품 생성 (IAuditable)
var product = Product.Create("노트북", 1_500_000m);
Console.WriteLine($"상품 생성: {product.Name}");
Console.WriteLine($"  CreatedAt: {product.CreatedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"  UpdatedAt: {product.UpdatedAt}");
Console.WriteLine($"  IsDeleted: {product.IsDeleted}");
Console.WriteLine();

// 2. 가격 변경 (UpdatedAt 갱신)
product.UpdatePrice(1_350_000m);
Console.WriteLine($"가격 변경 후:");
Console.WriteLine($"  Price: {product.Price:N0}원");
Console.WriteLine($"  UpdatedAt: {product.UpdatedAt}");
Console.WriteLine();

// 3. 소프트 삭제 (ISoftDeletableWithUser)
product.Delete("admin@example.com");
Console.WriteLine($"소프트 삭제 후:");
Console.WriteLine($"  IsDeleted: {product.IsDeleted}");
Console.WriteLine($"  DeletedAt: {product.DeletedAt}");
Console.WriteLine($"  DeletedBy: {product.DeletedBy}");
Console.WriteLine();

// 4. 복원
product.Restore();
Console.WriteLine($"복원 후:");
Console.WriteLine($"  IsDeleted: {product.IsDeleted}");
Console.WriteLine($"  DeletedAt: {product.DeletedAt}");
Console.WriteLine($"  UpdatedAt: {product.UpdatedAt}");
