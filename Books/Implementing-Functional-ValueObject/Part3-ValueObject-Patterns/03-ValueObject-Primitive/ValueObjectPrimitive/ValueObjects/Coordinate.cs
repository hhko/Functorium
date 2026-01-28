using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Domains.ValueObjects.Validations;
using static LanguageExt.Prelude;

namespace ValueObjectPrimitive.ValueObjects;

/// <summary>
/// 3. 비교 불가능한 복합 primitive 값 객체 - ValueObject
/// 2D 좌표를 나타내는 값 객체
/// DomainError 라이브러리를 사용한 간결한 구현
/// </summary>
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    private Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(Validate(x, y), v => new Coordinate(v.x, v.y));

    public static Coordinate CreateFromValidated((int x, int y) validatedValues) =>
        new(validatedValues.x, validatedValues.y);

    public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (x: validX, y: validY);

    private static Validation<Error, int> ValidateX(int x) =>
        ValidationRules<Coordinate>.NonNegative(x);

    private static Validation<Error, int> ValidateY(int y) =>
        ValidationRules<Coordinate>.Between(y, 0, 1000);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => $"({X}, {Y})";
}
