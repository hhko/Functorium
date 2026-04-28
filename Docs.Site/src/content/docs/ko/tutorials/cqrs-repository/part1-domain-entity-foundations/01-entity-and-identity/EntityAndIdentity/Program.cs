using EntityAndIdentity;

Console.WriteLine("=== Entity와 Identity ===\n");

// 1. ID 생성
var id1 = ProductId.New();
var id2 = ProductId.New();
Console.WriteLine($"ProductId 1: {id1}");
Console.WriteLine($"ProductId 2: {id2}");
Console.WriteLine($"동일한 ID인가? {id1.Equals(id2)}");
Console.WriteLine();

// 2. 문자열에서 ID 복원
var idString = id1.ToString();
var restored = ProductId.Create(idString);
Console.WriteLine($"원본 ID:  {id1}");
Console.WriteLine($"복원된 ID: {restored}");
Console.WriteLine($"동일한 ID인가? {id1.Equals(restored)}");
Console.WriteLine();

// 3. Entity 동등성 비교
var product1 = Product.Create("노트북", 1_500_000m);
var product2 = Product.Create("노트북", 1_500_000m);
Console.WriteLine($"Product 1 ID: {product1.Id}");
Console.WriteLine($"Product 2 ID: {product2.Id}");
Console.WriteLine($"같은 이름/가격이지만 다른 Entity인가? {product1 != product2}");
Console.WriteLine();

// 4. 같은 ID로 Entity 생성 시 동등성
var sharedId = ProductId.New();
var productA = Product.CreateFromValidated(sharedId, "마우스", 25_000m);
var productB = Product.CreateFromValidated(sharedId, "마우스", 25_000m);
Console.WriteLine($"같은 ID로 생성한 두 Entity가 동등한가? {productA == productB}");
Console.WriteLine($"GetHashCode 동일한가? {productA.GetHashCode() == productB.GetHashCode()}");
