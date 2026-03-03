using Functorium.Applications.Usecases;
using LanguageExt.Common;

namespace FullPipelineIntegration;

/// <summary>
/// 7개 Pipeline의 전체 흐름을 시뮬레이션합니다.
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
        Func<TResponse> handler)
    {
        // 1. Exception Pipeline
        TResponse response;
        try
        {
            // 2. Validation Pipeline
            if (!isValid)
            {
                ExecutionLog.Add("Validation: FAIL");
                return TResponse.CreateFail(Error.New("Validation failed"));
            }
            ExecutionLog.Add("Validation: PASS");

            // 3. Logging Pipeline (before)
            ExecutionLog.Add("Logging: Request received");

            // 4. Tracing Pipeline (before)
            ExecutionLog.Add("Tracing: Activity started");

            // 5. Metrics Pipeline (before)
            ExecutionLog.Add("Metrics: Request count++");

            // 6. Transaction Pipeline (Command only)
            if (isCommand)
                ExecutionLog.Add("Transaction: BEGIN");

            // 7. Handler
            response = handler();
            ExecutionLog.Add($"Handler: executed (IsSucc={response.IsSucc})");

            // 6. Transaction Pipeline (after)
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

        // 5. Metrics Pipeline (after)
        ExecutionLog.Add($"Metrics: Response count++ (success={response.IsSucc})");

        // 4. Tracing Pipeline (after)
        ExecutionLog.Add($"Tracing: Activity completed (status={(response.IsSucc ? "OK" : "ERROR")})");

        // 3. Logging Pipeline (after)
        if (response.IsSucc)
            ExecutionLog.Add("Logging: Success");
        else if (response is IFinResponseWithError fail)
            ExecutionLog.Add($"Logging: Fail - {fail.Error}");

        return response;
    }
}
