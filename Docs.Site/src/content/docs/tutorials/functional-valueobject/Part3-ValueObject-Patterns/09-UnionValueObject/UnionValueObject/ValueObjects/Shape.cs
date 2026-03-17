namespace UnionValueObject.ValueObjects;

/// <summary>
/// 도형을 표현하는 Discriminated Union 값 객체.
/// Circle | Rectangle | Triangle 중 정확히 하나.
/// </summary>
public abstract record Shape : UnionValueObject
{
    public sealed record Circle(double Radius) : Shape;
    public sealed record Rectangle(double Width, double Height) : Shape;
    public sealed record Triangle(double Base, double Height) : Shape;

    // --- 수동 Match/Switch 구현 (교육용) ---

    public TResult Match<TResult>(
        Func<Circle, TResult> circle,
        Func<Rectangle, TResult> rectangle,
        Func<Triangle, TResult> triangle) => this switch
    {
        Circle c => circle(c),
        Rectangle r => rectangle(r),
        Triangle t => triangle(t),
        _ => throw new UnreachableCaseException(this)
    };

    public void Switch(
        Action<Circle> circle,
        Action<Rectangle> rectangle,
        Action<Triangle> triangle)
    {
        switch (this)
        {
            case Circle c: circle(c); break;
            case Rectangle r: rectangle(r); break;
            case Triangle t: triangle(t); break;
            default: throw new UnreachableCaseException(this);
        }
    }

    // --- 도메인 로직 ---

    public double Area => Match(
        circle: c => Math.PI * c.Radius * c.Radius,
        rectangle: r => r.Width * r.Height,
        triangle: t => 0.5 * t.Base * t.Height);

    public double Perimeter => Match(
        circle: c => 2 * Math.PI * c.Radius,
        rectangle: r => 2 * (r.Width + r.Height),
        triangle: t => t.Base + t.Height + Math.Sqrt(t.Base * t.Base + t.Height * t.Height));
}
