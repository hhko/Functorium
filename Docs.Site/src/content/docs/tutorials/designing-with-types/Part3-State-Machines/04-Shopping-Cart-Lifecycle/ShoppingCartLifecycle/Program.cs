using ShoppingCartLifecycle;

namespace ShoppingCartLifecycle;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== 쇼핑 카트 라이프사이클 ===\n");

        // Empty → Active (아이템 추가)
        ShoppingCart cart = new ShoppingCart.Empty();
        Console.WriteLine($"시작: {cart.GetType().Name}");

        var addResult = ShoppingCart.AddItem(cart, "Widget");
        addResult.Match(
            Succ: c =>
            {
                Console.WriteLine($"아이템 추가 후: {c.GetType().Name}");

                // Active → Paid (결제)
                var payResult = ShoppingCart.Pay(c, 29.99m);
                payResult.Match(
                    Succ: paid => Console.WriteLine($"결제 후: {paid.GetType().Name}"),
                    Fail: e => Console.WriteLine($"결제 실패: {e.Message}"));
            },
            Fail: e => Console.WriteLine($"추가 실패: {e.Message}"));

        // Empty → Paid 직접 전이 시도 (실패)
        Console.WriteLine("\nEmpty에서 Paid로 직접 전이 시도:");
        var directPay = ShoppingCart.Pay(new ShoppingCart.Empty(), 29.99m);
        directPay.Match(
            Succ: _ => Console.WriteLine("성공 (이것은 발생하면 안 됨)"),
            Fail: e => Console.WriteLine($"실패: {e.Message}"));
    }
}
