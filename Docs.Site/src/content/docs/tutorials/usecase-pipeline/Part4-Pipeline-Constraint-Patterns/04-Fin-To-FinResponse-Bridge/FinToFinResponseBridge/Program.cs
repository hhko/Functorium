using FinToFinResponseBridge;

// 1. 직접 변환
var direct = BridgeExamples.DirectConversion();
Console.WriteLine($"Direct: {direct}");

// 2. 매퍼 변환
var mapped = BridgeExamples.MappedConversion();
Console.WriteLine($"Mapped: {mapped}");

// 3. 팩토리 변환
var factory = BridgeExamples.FactoryConversion();
Console.WriteLine($"Factory: {factory}");

// 4. 실패 변환
var fail = BridgeExamples.FailConversion();
Console.WriteLine($"Fail: {fail}");

// 5. 커스텀 변환
var custom = BridgeExamples.CustomConversion();
Console.WriteLine($"Custom: {custom}");
