using Functorium.Applications.Usecases;
using LanguageExt.Common;
using ReadCreateConstraint;

// Logging Pipeline
var loggingPipeline = new SimpleLoggingPipeline<FinResponse<string>>();

var success = FinResponse.Succ("Hello");
loggingPipeline.LogAndReturn(success);
Console.WriteLine($"Logs after success: {string.Join(", ", loggingPipeline.Logs)}");

var fail = FinResponse.Fail<string>(Error.New("bad request"));
loggingPipeline.LogAndReturn(fail);
Console.WriteLine($"Logs after fail: {string.Join(", ", loggingPipeline.Logs)}");

// Tracing Pipeline
var tracingPipeline = new SimpleTracingPipeline<FinResponse<string>>();

tracingPipeline.TraceAndReturn(success);
tracingPipeline.TraceAndReturn(fail);
Console.WriteLine($"Tags: {string.Join(", ", tracingPipeline.Tags)}");
