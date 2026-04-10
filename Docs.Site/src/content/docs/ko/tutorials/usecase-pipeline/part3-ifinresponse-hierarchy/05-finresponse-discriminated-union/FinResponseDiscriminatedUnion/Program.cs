using LanguageExt.Common;
using FinResponseDiscriminatedUnion;

// 정적 팩토리로 생성
FinResponse<int> succ = FinResponse.Succ(42);
FinResponse<int> fail = FinResponse.Fail<int>(Error.New("error"));

// Match
var succResult = succ.Match(Succ: v => $"값: {v}", Fail: e => $"에러: {e}");
var failResult = fail.Match(Succ: v => $"값: {v}", Fail: e => $"에러: {e}");
Console.WriteLine(succResult);
Console.WriteLine(failResult);

// Map
var mapped = succ.Map(v => v * 2);
Console.WriteLine(mapped);

// 암시적 변환
FinResponse<string> implicitSucc = "Hello";
FinResponse<string> implicitFail = Error.New("implicit error");
Console.WriteLine(implicitSucc);
Console.WriteLine(implicitFail);

// LINQ
var linq = from x in FinResponse.Succ(3)
           from y in FinResponse.Succ(4)
           select x + y;
Console.WriteLine(linq);

// ThrowIfFail
var value = succ.ThrowIfFail();
Console.WriteLine($"ThrowIfFail: {value}");

// IfFail
var withFallback = fail.IfFail(-1);
Console.WriteLine($"IfFail(value): {withFallback}");

var withFunc = fail.IfFail(err => 0);
Console.WriteLine($"IfFail(func): {withFunc}");

// Boolean operator
if (succ)
    Console.WriteLine("succ is true");

if (fail) { }
else Console.WriteLine("fail is false");

// Choice operator
var choice = fail | FinResponse.Succ(99);
Console.WriteLine($"Choice: {choice}");
