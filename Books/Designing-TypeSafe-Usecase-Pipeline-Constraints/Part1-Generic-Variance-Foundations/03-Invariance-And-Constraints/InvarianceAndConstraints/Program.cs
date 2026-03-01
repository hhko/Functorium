using LanguageExt;
using LanguageExt.Common;
using InvarianceAndConstraints;

// 1. 불변성: List<T>는 공변/반공변 불가
Console.WriteLine("=== 불변성 ===");
var animals = InvarianceExamples.GetAnimals().ToList();
foreach (var animal in animals)
    Console.WriteLine($"  {animal.Name}");

// 2. sealed struct (Fin<T>) 사용
Console.WriteLine("\n=== Fin<T> sealed struct ===");
Fin<string> success = Fin.Succ("Hello");
Fin<string> failure = Fin.Fail<string>(Error.New("Something went wrong"));

Console.WriteLine($"  Success: {SealedStructConstraint.ProcessFin(success)}");
Console.WriteLine($"  Failure: {SealedStructConstraint.ProcessFin(failure)}");

// 3. 인터페이스 제약으로 우회
Console.WriteLine("\n=== 인터페이스 제약 ===");
var successResult = new SuccessResult("Data");
var failResult = new FailResult("Error");

Console.WriteLine($"  SuccessResult: {SealedStructConstraint.ProcessResult(successResult)}");
Console.WriteLine($"  FailResult: {SealedStructConstraint.ProcessResult(failResult)}");
