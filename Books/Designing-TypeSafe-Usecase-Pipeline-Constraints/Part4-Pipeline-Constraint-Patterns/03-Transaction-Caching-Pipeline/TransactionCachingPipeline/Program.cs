using Functorium.Applications.Usecases;
using LanguageExt.Common;
using TransactionCachingPipeline;

// Transaction Pipeline
var transactionPipeline = new SimpleTransactionPipeline<FinResponse<string>>();

// Command: 트랜잭션 적용
transactionPipeline.Execute(
    isCommand: true,
    handler: () => FinResponse.Succ("Created"));
Console.WriteLine($"Transaction actions: {string.Join(" → ", transactionPipeline.Actions)}");

// Query: 트랜잭션 건너뛰기
transactionPipeline.Execute(
    isCommand: false,
    handler: () => FinResponse.Succ("Data"));
Console.WriteLine($"Transaction actions: {string.Join(" → ", transactionPipeline.Actions)}");

// Caching Pipeline
var cachingPipeline = new SimpleCachingPipeline<FinResponse<string>>();

var callCount = 0;
FinResponse<string> Handler()
{
    callCount++;
    return FinResponse.Succ("Cached Data");
}

// 첫 번째 호출: 캐시 미스 → 핸들러 실행
cachingPipeline.GetOrExecute("key1", isCacheable: true, Handler);
Console.WriteLine($"Call count after 1st: {callCount}");

// 두 번째 호출: 캐시 히트 → 핸들러 실행 안 함
cachingPipeline.GetOrExecute("key1", isCacheable: true, Handler);
Console.WriteLine($"Call count after 2nd: {callCount}");
