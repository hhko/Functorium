using System.Collections.Concurrent;

namespace Functorium.Testing.Arrangements.ScheduledJobs;

/// <summary>
/// Job 완료를 감지하고 비동기 대기를 지원하는 IJobListener 구현체
/// </summary>
public sealed class JobCompletionListener : IJobListener
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JobExecutionResult>> _completionSources = new();

    public string Name => nameof(JobCompletionListener);

    /// <summary>
    /// 지정된 Job이 완료될 때까지 대기합니다.
    /// </summary>
    /// <param name="jobName">대기할 Job 이름</param>
    /// <param name="timeout">최대 대기 시간</param>
    /// <param name="cancellationToken">취소 토큰</param>
    /// <returns>Job 실행 결과</returns>
    /// <exception cref="TimeoutException">타임아웃 시 발생</exception>
    public async Task<JobExecutionResult> WaitForJobCompletionAsync(
        string jobName,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var tcs = _completionSources.GetOrAdd(jobName, _ => new TaskCompletionSource<JobExecutionResult>());

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout, cancellationToken));

        if (completedTask != tcs.Task)
        {
            throw new TimeoutException($"Job '{jobName}'이(가) {timeout.TotalSeconds}초 내에 완료되지 않았습니다.");
        }

        return await tcs.Task;
    }

    /// <summary>
    /// 상태를 초기화합니다. 각 테스트 전에 호출해야 합니다.
    /// </summary>
    public void Reset()
    {
        _completionSources.Clear();
    }

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        var jobName = context.JobDetail.Key.Name;
        _completionSources.GetOrAdd(jobName, _ => new TaskCompletionSource<JobExecutionResult>());
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException? jobException, CancellationToken cancellationToken = default)
    {
        var jobName = context.JobDetail.Key.Name;

        if (_completionSources.TryGetValue(jobName, out var tcs))
        {
            var result = new JobExecutionResult(
                JobName: jobName,
                Success: jobException is null,
                Result: context.Result,
                Exception: jobException,
                ExecutionTime: context.JobRunTime);

            tcs.TrySetResult(result);
        }

        return Task.CompletedTask;
    }
}
