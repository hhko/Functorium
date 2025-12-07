namespace Functorium.Testing.Arrangements.ScheduledJobs;

/// <summary>
/// Job 실행 결과를 나타내는 레코드
/// </summary>
/// <param name="JobName">실행된 Job 이름</param>
/// <param name="Success">성공 여부 (예외 없이 완료되면 true)</param>
/// <param name="Result">Job 실행 결과 (context.Result)</param>
/// <param name="Exception">Job 실행 중 발생한 예외</param>
/// <param name="ExecutionTime">Job 실행 시간</param>
public sealed record JobExecutionResult(
    string JobName,
    bool Success,
    object? Result,
    JobExecutionException? Exception,
    TimeSpan ExecutionTime);