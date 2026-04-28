namespace ApplicationLayerRules.Applications;

public sealed class CreateOrder
{
    public sealed record Request(string CustomerName);
    public sealed record Response(Guid OrderId, bool Success);

    public sealed class Usecase : ICommandUsecase<Request>
    {
        public Task ExecuteAsync(Request request) => Task.CompletedTask;
    }
}
