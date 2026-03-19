using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace FullPipelineIntegration;

/// <summary>
/// 8개 슬롯(7개 기본 + Custom)의 전체 흐름을 시뮬레이션합니다.
/// 실제 Mediator Pipeline은 DI로 자동 등록되지만,
/// 여기서는 학습 목적으로 수동 호출합니다.
/// </summary>
public sealed class PipelineOrchestrator<TResponse>
    where TResponse : IFinResponse, IFinResponseFactory<TResponse>
{
    public List<string> ExecutionLog { get; } = [];

    public TResponse Execute(
        bool isValid,
        bool isCommand,
        Func<TResponse> handler,
        Func<TResponse, TResponse>? customPipeline = null)
    {
        // 1. Metrics Pipeline (outer — 항상 실행)
        ExecutionLog.Add("Metrics: Request count++");

        // 2. Tracing Pipeline (outer — 항상 실행)
        ExecutionLog.Add("Tracing: Activity started");

        // 3. Logging Pipeline (outer — 항상 실행)
        ExecutionLog.Add("Logging: Request received");

        // 4. Validation Pipeline
        if (!isValid)
        {
            ExecutionLog.Add("Validation: FAIL");
            var failResponse = TResponse.CreateFail(Error.New("Validation failed"));
            LogAfterPipelines(failResponse);
            return failResponse;
        }
        ExecutionLog.Add("Validation: PASS");

        // 5. Exception Pipeline (try/catch가 Transaction+Handler 감싸기)
        TResponse response;
        try
        {
            // 6. Transaction Pipeline (Command only)
            if (isCommand)
                ExecutionLog.Add("Transaction: BEGIN");

            // 7. Custom Pipeline (optional, before handler)
            if (customPipeline is not null)
                ExecutionLog.Add("Custom: before handler");

            // 8. Handler
            response = handler();
            ExecutionLog.Add($"Handler: executed (IsSucc={response.IsSucc})");

            // 7. Custom Pipeline (after handler)
            if (customPipeline is not null)
            {
                response = customPipeline(response);
                ExecutionLog.Add("Custom: after handler");
            }

            // 6. Transaction Pipeline (after handler)
            if (isCommand)
            {
                if (response.IsSucc)
                    ExecutionLog.Add("Transaction: COMMIT");
                else
                    ExecutionLog.Add("Transaction: ROLLBACK");
            }
        }
        catch (Exception ex)
        {
            ExecutionLog.Add($"Exception: {ex.Message}");
            response = TResponse.CreateFail(Error.New(ex));
        }

        // 3→2→1 후처리
        LogAfterPipelines(response);
        return response;
    }

    private void LogAfterPipelines(TResponse response)
    {
        // 3. Logging Pipeline (after)
        if (response.IsSucc)
            ExecutionLog.Add("Logging: Success");
        else if (response is IFinResponseWithError fail)
            ExecutionLog.Add($"Logging: Fail - {fail.Error}");

        // 2. Tracing Pipeline (after)
        ExecutionLog.Add($"Tracing: Activity completed (status={(response.IsSucc ? "OK" : "ERROR")})");

        // 1. Metrics Pipeline (after)
        ExecutionLog.Add($"Metrics: Response count++ (success={response.IsSucc})");
    }
}
