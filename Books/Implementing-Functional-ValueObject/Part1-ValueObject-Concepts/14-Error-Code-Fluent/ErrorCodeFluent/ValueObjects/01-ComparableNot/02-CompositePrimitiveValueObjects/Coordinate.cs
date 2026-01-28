using Functorium.Domains.ValueObjects;
using Functorium.Domains.Errors;
using LanguageExt;
using LanguageExt.Common;

namespace ErrorCodeFluent.ValueObjects.ComparableNot.CompositePrimitiveValueObjects;

/// <summary>
/// 좌표를 나타내는 복합 값 객체
/// DomainError 헬퍼를 사용한 간결한 에러 처리
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
        CreateFromValidation(Validate(x, y), validValues => new Coordinate(validValues.X, validValues.Y));

    public static Coordinate CreateFromValidated(int x, int y) =>
        new Coordinate(x, y);

    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    private static Validation<Error, int> ValidateX(int x) =>
        x < 0 || x > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), x,
                $"X coordinate must be between 0 and 1000. Current value: '{x}'")
            : x;

    private static Validation<Error, int> ValidateY(int y) =>
        y < 0 || y > 1000
            ? DomainError.For<Coordinate, int>(new DomainErrorType.OutOfRange("0", "1000"), y,
                $"Y coordinate must be between 0 and 1000. Current value: '{y}'")
            : y;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => $"({X}, {Y})";
}
