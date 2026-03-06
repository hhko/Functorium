namespace AdapterLayerRules.Domains.Ports;

public interface INotificationService
{
    Task SendAsync(string message);
}
