namespace ApplicationLayerRules.Applications;

public interface ICommandUsecase<TRequest>
{
    Task ExecuteAsync(TRequest request);
}
