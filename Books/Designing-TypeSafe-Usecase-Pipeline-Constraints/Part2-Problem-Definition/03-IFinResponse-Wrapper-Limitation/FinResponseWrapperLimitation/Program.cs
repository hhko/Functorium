using LanguageExt.Common;
using FinResponseWrapperLimitation;

// 래퍼를 통한 접근 예제
var success = ResponseWrapper<TestResponse>.Success(new TestResponse("OK"));
var fail = ResponseWrapper<TestResponse>.Fail(Error.New("Something went wrong"));

Console.WriteLine($"Success: {WrapperPipelineExample.ProcessResponse(success)}");
Console.WriteLine($"Fail: {WrapperPipelineExample.ProcessResponse(fail)}");
Console.WriteLine($"Unknown: {WrapperPipelineExample.ProcessResponse("plain string")}");

file record TestResponse(string Message) : IResponse;
