using LanguageExt;
using UnitOfWork;

Console.WriteLine("=== Unit of Work ===\n");

// 1. InMemoryUnitOfWork 생성
var uow = new InMemoryUnitOfWork();
Console.WriteLine($"초기 상태 - IsSaved: {uow.IsSaved}");
Console.WriteLine();

// 2. 대기 중인 작업 등록
var store = new Dictionary<string, Product>();
var product = Product.Create("키보드", 89_000m);

uow.AddPendingAction(() => store[product.Id.ToString()] = product);
Console.WriteLine($"AddPendingAction 등록 후 - Store 개수: {store.Count}");
Console.WriteLine($"  (아직 SaveChanges 전이므로 Store는 비어 있음)");
Console.WriteLine();

// 3. SaveChanges 실행
var result = await uow.SaveChanges().Run().RunAsync();
Console.WriteLine($"SaveChanges 후 - IsSucc: {result.IsSucc}");
Console.WriteLine($"  IsSaved: {uow.IsSaved}");
Console.WriteLine($"  Store 개수: {store.Count}");
Console.WriteLine($"  저장된 상품: {store.Values.First().Name} ({store.Values.First().Price:N0}원)");
Console.WriteLine();

// 4. 트랜잭션
await using (var tx = await uow.BeginTransactionAsync())
{
    Console.WriteLine("트랜잭션 시작");
    var product2 = Product.Create("마우스", 35_000m);
    store[product2.Id.ToString()] = product2;
    await tx.CommitAsync();
    Console.WriteLine($"  트랜잭션 커밋 완료 - Store 개수: {store.Count}");
}
Console.WriteLine("트랜잭션 Dispose 완료");

// 5. 다중 Aggregate 트랜잭션: 주문 생성 + 재고 차감
Console.WriteLine("\n=== 다중 Aggregate 트랜잭션 ===\n");

var productStore = new Dictionary<ProductId, Product>();
var orderStore = new Dictionary<OrderId, Order>();

var laptop = Product.Create("노트북", 1_500_000m, stock: 10);
productStore[laptop.Id] = laptop;

var uow2 = new InMemoryUnitOfWork();

// 두 Aggregate의 변경을 하나의 UoW에 등록
var order = Order.Create(laptop.Id, quantity: 2, unitPrice: laptop.Price);
uow2.AddPendingAction(() => orderStore[order.Id] = order);
uow2.AddPendingAction(() => laptop.DeductStock(2));

Console.WriteLine($"SaveChanges 전 - 주문: {orderStore.Count}건, 재고: {laptop.Stock}");

var result2 = await uow2.SaveChanges().Run().RunAsync();

Console.WriteLine($"SaveChanges 후 - 주문: {orderStore.Count}건, 재고: {laptop.Stock}");
Console.WriteLine($"  주문 금액: {order.TotalAmount:N0}원");
