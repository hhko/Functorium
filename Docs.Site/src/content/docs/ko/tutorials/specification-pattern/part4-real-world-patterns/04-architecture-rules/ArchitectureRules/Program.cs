Console.WriteLine("=== Architecture Rules for Specifications ===\n");

Console.WriteLine("Specification 아키텍처 규칙:\n");

Console.WriteLine("1. 네이밍 규칙: {Aggregate}{Condition}Spec");
Console.WriteLine("   - ProductInStockSpec");
Console.WriteLine("   - ProductPriceRangeSpec");
Console.WriteLine("   - ProductLowStockSpec\n");

Console.WriteLine("2. 폴더 배치: Aggregate 하위 Specifications/ 폴더");
Console.WriteLine("   Domain/AggregateRoots/Products/");
Console.WriteLine("   ├── Product.cs");
Console.WriteLine("   └── Specifications/");
Console.WriteLine("       ├── ProductInStockSpec.cs");
Console.WriteLine("       ├── ProductPriceRangeSpec.cs");
Console.WriteLine("       └── ProductLowStockSpec.cs\n");

Console.WriteLine("3. ArchUnitNET으로 자동 검증:");
Console.WriteLine("   - Specifications 네임스페이스의 클래스는 Spec으로 끝나야 함");
Console.WriteLine("   - Spec으로 끝나는 클래스는 Specifications 네임스페이스에 있어야 함");
