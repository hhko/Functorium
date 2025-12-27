using Framework.Layers.Domains;
using LanguageExt;
using LanguageExt.Common;

namespace ValueObjectFramework.ValueObjects.ComparableNot.CompositePrimitiveValueObjects;

/// <summary>
/// 좌표를 나타내는 복합 값 객체 (2개 Validation 조합 예제)
/// 10-Validated-Value-Creation 패턴 적용
/// </summary>
public sealed class Coordinate : ValueObject
{
    public int X { get; }
    public int Y { get; }

    /// <summary>
    /// Coordinate 인스턴스를 생성하는 private 생성자
    /// 직접 인스턴스 생성 방지
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    private Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    /// <summary>
    /// Coordinate 인스턴스를 생성하는 팩토리 메서드
    /// 부모 클래스의 CreateFromValidation 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    /// <returns>성공 시 Coordinate, 실패 시 Error</returns>
    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(
            Validate(x, y),
            validValues => new Coordinate(validValues.X, validValues.Y));

    /// <summary>
    /// 이미 검증된 값으로 Coordinate 인스턴스를 생성하는 static internal 메서드
    /// 부모 값 객체에서만 사용
    /// </summary>
    /// <param name="x">이미 검증된 X 좌표</param>
    /// <param name="y">이미 검증된 Y 좌표</param>
    /// <returns>생성된 Coordinate 인스턴스</returns>
    internal static Coordinate CreateFromValidated(int x, int y) =>
        new Coordinate(x, y);

    /// <summary>
    /// 검증 책임 - 단일 책임 원칙
    /// 부모 클래스의 CombineValidations 헬퍼를 활용하여 간결하게 구현
    /// </summary>
    /// <param name="x">검증할 X 좌표</param>
    /// <param name="y">검증할 Y 좌표</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (int X, int Y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (X: validX, Y: validY);

    /// <summary>
    /// X 좌표 검증
    /// </summary>
    /// <param name="x">검증할 X 좌표</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, int> ValidateX(int x) =>
        x < 0 || x > 1000
            ? Error.New("X 좌표는 0-1000 범위여야 합니다")
            : x;

    /// <summary>
    /// Y 좌표 검증
    /// </summary>
    /// <param name="y">검증할 Y 좌표</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, int> ValidateY(int y) =>
        y < 0 || y > 1000
            ? Error.New("Y 좌표는 0-1000 범위여야 합니다")
            : y;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() =>
        $"({X}, {Y})";
}
