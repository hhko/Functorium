using LanguageExt;
using LanguageExt.Common;
using FinDirectLimitation;

// Fin<T> 사용 예제
Fin<string> success = "Hello";
Fin<string> fail = Error.New("Something went wrong");

// 리플렉션 기반 접근 (Pipeline에서 이렇게 해야 함)
Console.WriteLine($"Success IsSucc: {FinReflectionUtility.IsSucc(success)}");
Console.WriteLine($"Fail IsSucc: {FinReflectionUtility.IsSucc(fail)}");
