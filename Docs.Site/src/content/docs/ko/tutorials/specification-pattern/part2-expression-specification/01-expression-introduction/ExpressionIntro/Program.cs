using System.Linq.Expressions;
using ExpressionIntro;

Console.WriteLine("=== Expression Tree 기초 ===\n");

// --- Func: 불투명한 블랙박스 ---
Console.WriteLine("▶ Func - 불투명한 블랙박스:");
Func<Product, bool> func = p => p.Price > 1000;
Console.WriteLine($"  Type: {func.GetType().Name}");
Console.WriteLine($"  (내부 구조를 검사할 수 없음)");

Console.WriteLine();

// --- Expression: 검사 가능한 트리 ---
Console.WriteLine("▶ Expression - 검사 가능한 트리:");
Expression<Func<Product, bool>> expr = p => p.Price > 1000;
Console.WriteLine($"  Body: {expr.Body}");
Console.WriteLine($"  Parameters: {string.Join(", ", expr.Parameters)}");
Console.WriteLine($"  NodeType: {expr.Body.NodeType}");

Console.WriteLine();

// --- Expression → Func 컴파일 ---
Console.WriteLine("▶ Expression → Func 컴파일:");
var compiled = expr.Compile();
var product = new Product("노트북", 1_500_000, 10, "전자제품");
Console.WriteLine($"  Product: {product.Name} ({product.Price:N0}원)");
Console.WriteLine($"  Result: {compiled(product)}");

Console.WriteLine();

// --- AsQueryable에서 Expression 활용 ---
Console.WriteLine("▶ AsQueryable + Expression:");
var products = new List<Product>
{
    new("노트북", 1_500_000, 10, "전자제품"),
    new("마우스", 25_000, 50, "전자제품"),
    new("키보드", 80_000, 30, "전자제품"),
    new("볼펜", 500, 100, "문구류"),
};

var expensive = products.AsQueryable().Where(expr);
foreach (var p in expensive)
    Console.WriteLine($"  {p.Name} ({p.Price:N0}원)");
