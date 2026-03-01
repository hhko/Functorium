using LanguageExt.Common;
using FinResponseWithError;

var success = ErrorAccessResponse<string>.CreateSucc("Hello");
var fail = ErrorAccessResponse<string>.CreateFail(Error.New("bad request"));

Console.WriteLine(LoggingPipelineExample.LogResponse(success));
Console.WriteLine(LoggingPipelineExample.LogResponse(fail));

// 패턴 매칭으로 에러 접근
if (fail is IFinResponseWithError failWithError)
    Console.WriteLine($"Error: {failWithError.Error}");

// Succ는 IFinResponseWithError를 구현하지 않음
Console.WriteLine($"Succ is IFinResponseWithError: {success is IFinResponseWithError}");
