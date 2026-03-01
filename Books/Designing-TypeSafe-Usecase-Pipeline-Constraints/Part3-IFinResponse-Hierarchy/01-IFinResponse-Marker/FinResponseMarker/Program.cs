using LanguageExt.Common;
using FinResponseMarker;

var success = SimpleResponse<string>.Succ("Hello");
var fail = SimpleResponse<string>.Fail(Error.New("error"));

Console.WriteLine(PipelineExample.LogResponse(success));
Console.WriteLine(PipelineExample.LogResponse(fail));
