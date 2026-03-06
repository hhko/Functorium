using LanguageExt;
using LanguageExt.Common;

namespace InvarianceAndConstraints;

/// <summary>
/// List<T>는 불변 - 공변도 반공변도 아님
/// </summary>
public static class InvarianceExamples
{
    // List<Dog>를 List<Animal>에 대입할 수 없음 (불변)
    // List<Animal> animals = new List<Dog>();  // 컴파일 에러!

    // 하지만 IEnumerable<out T>로는 가능 (공변)
    public static IEnumerable<Animal> GetAnimals()
    {
        List<Dog> dogs = [new Dog("Buddy")];
        return dogs;  // IEnumerable<out T>이므로 OK
    }
}

public class Animal(string Name)
{
    public string Name { get; } = Name;
}

public class Dog(string Name) : Animal(Name);

/// <summary>
/// sealed struct 제약의 한계를 보여주는 예제
/// Fin<T>는 sealed struct이므로 where 제약으로 사용 불가
/// </summary>
public static class SealedStructConstraint
{
    // 이 메서드는 Fin<T>의 결과를 처리하지만,
    // where TResponse : Fin<T> 같은 제약은 불가능합니다
    public static string ProcessFin(Fin<string> fin) =>
        fin.Match(
            Succ: value => $"Success: {value}",
            Fail: error => $"Fail: {error}");

    // 인터페이스 제약은 가능합니다
    public static string ProcessResult<T>(T result) where T : IResult
    {
        return result.IsSucc ? "Success" : "Fail";
    }
}

/// <summary>
/// 인터페이스 제약은 가능 - sealed struct의 한계를 우회하는 방법
/// </summary>
public interface IResult
{
    bool IsSucc { get; }
    bool IsFail { get; }
}

public record SuccessResult(string Value) : IResult
{
    public bool IsSucc => true;
    public bool IsFail => false;
}

public record FailResult(string ErrorMessage) : IResult
{
    public bool IsSucc => false;
    public bool IsFail => true;
}
