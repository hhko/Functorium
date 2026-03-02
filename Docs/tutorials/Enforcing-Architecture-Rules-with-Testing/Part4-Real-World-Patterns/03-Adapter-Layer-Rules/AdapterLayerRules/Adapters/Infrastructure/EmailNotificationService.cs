using AdapterLayerRules.Domains.Ports;

namespace AdapterLayerRules.Adapters.Infrastructure;

public sealed class EmailNotificationService : INotificationService
{
    public Task SendAsync(string message) => Task.CompletedTask;
}
