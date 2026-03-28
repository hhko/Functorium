using Functorium.Applications.Usecases;
using FullPipelineIntegration;

var orchestrator = new PipelineOrchestrator<FinResponse<string>>();

// Command 성공 시나리오
var result = orchestrator.Execute(
    isValid: true,
    isCommand: true,
    handler: () => FinResponse.Succ("Product created"));

Console.WriteLine($"Result: {result}");
Console.WriteLine();
foreach (var log in orchestrator.ExecutionLog)
    Console.WriteLine($"  {log}");
