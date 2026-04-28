namespace ApplicationLayerRules.Applications;

public interface IQueryUsecase<TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request);
}
