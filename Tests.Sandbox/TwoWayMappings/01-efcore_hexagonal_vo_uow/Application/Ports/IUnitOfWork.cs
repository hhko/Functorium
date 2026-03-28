namespace MyApp.Application.Ports;

public interface IUnitOfWork
{
    Task ExecuteAsync(Func<CancellationToken, Task> action, CancellationToken ct = default);
}
