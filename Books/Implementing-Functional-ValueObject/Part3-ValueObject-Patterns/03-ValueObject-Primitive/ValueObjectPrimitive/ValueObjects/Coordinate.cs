using LanguageExt;
using LanguageExt.Common;
using Functorium.Domains.ValueObjects;
using Functorium.Abstractions.Errors;
using static LanguageExt.Prelude;

namespace ValueObjectPrimitive.ValueObjects;

/// <summary>
/// 3. 비교 불가능한 복합 primitive 값 객체 - ValueObject
/// 2D 좌표를 나타내는 값 객체
/// 
/// 특징:
/// - 여러 primitive 값을 조합
/// - 동등성 비교만 제공
/// - 비교 기능은 제공되지 않음 (의도적으로)
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

    /// <summary>
    /// 좌표 값 객체 생성
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    /// <returns>성공 시 Coordinate 값 객체, 실패 시 에러</returns>
    public static Fin<Coordinate> Create(int x, int y) =>
        CreateFromValidation(
            Validate(x, y),
            validValues => new Coordinate(validValues.x, validValues.y));

    /// <summary>
    /// 이미 검증된 좌표로 값 객체 생성
    /// </summary>
    /// <param name="validatedValues">검증된 좌표 값들</param>
    /// <returns>Coordinate 값 객체</returns>
    internal static Coordinate CreateFromValidated((int x, int y) validatedValues) =>
        new Coordinate(validatedValues.x, validatedValues.y);

    /// <summary>
    /// 좌표 유효성 검증
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    /// <returns>검증 결과</returns>
    public static Validation<Error, (int x, int y)> Validate(int x, int y) =>
        from validX in ValidateX(x)
        from validY in ValidateY(y)
        select (x: validX, y: validY);

    /// <summary>
    /// X 좌표 검증
    /// </summary>
    /// <param name="x">검증할 X 좌표</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, int> ValidateX(int x) =>
        x >= 0
            ? x
            : DomainErrors.XOutOfRange(x);

    /// <summary>
    /// Y 좌표 검증
    /// </summary>
    /// <param name="y">검증할 Y 좌표</param>
    /// <returns>검증 결과</returns>
    private static Validation<Error, int> ValidateY(int y) =>
        y >= 0 && y <= 1000
            ? y
            : DomainErrors.YOutOfRange(y);

    /// <summary>
    /// 동등성 비교를 위한 구성 요소 반환
    /// </summary>
    /// <returns>동등성 비교 구성 요소</returns>
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return X;
        yield return Y;
    }

    public override string ToString() => 
        $"({X}, {Y})";

    internal static class DomainErrors
    {
        public static Error XOutOfRange(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(XOutOfRange)}",
                errorCurrentValue: value,
                errorMessage: $"X coordinate must be non-negative. Current value: '{value}'");

        public static Error YOutOfRange(int value) =>
            ErrorCodeFactory.Create(
                errorCode: $"{nameof(DomainErrors)}.{nameof(Coordinate)}.{nameof(YOutOfRange)}",
                errorCurrentValue: value,
                errorMessage: $"Y coordinate must be between 0 and 1000. Current value: '{value}'");
    }
}
