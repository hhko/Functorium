using Microsoft.Extensions.Logging;

namespace Functorium.Adapters.Observabilities.Loggers;

/// <summary>
/// 애플리케이션 시작 시 유스케이스 파이프라인 설정 정보를 로그로 출력합니다.
/// Command/Query 파이프라인 실행 순서를 표시합니다.
/// </summary>
internal class UsecasePipelineStartupLogger : IStartupOptionsLogger
{
    private readonly IReadOnlyList<string> _commandPipelineNames;
    private readonly IReadOnlyList<string> _queryPipelineNames;

    internal UsecasePipelineStartupLogger(
        IReadOnlyList<string> commandPipelineNames,
        IReadOnlyList<string> queryPipelineNames)
    {
        _commandPipelineNames = commandPipelineNames;
        _queryPipelineNames = queryPipelineNames;
    }

    public void LogConfiguration(ILogger logger)
    {
        logger.LogInformation("Usecase Pipeline Configuration");
        logger.LogInformation("");

        LogPipelineSection(logger, "Command", _commandPipelineNames);
        LogPipelineSection(logger, "Query", _queryPipelineNames);
    }

    private static void LogPipelineSection(
        ILogger logger,
        string pipelineType,
        IReadOnlyList<string> pipelineNames)
    {
        logger.LogInformation("  {PipelineType} Pipeline", pipelineType);
        logger.LogInformation("    {PipelineChain}", string.Join(" → ", pipelineNames));
        //logger.LogInformation("");
    }
}
