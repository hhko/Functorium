namespace CleanArchitecture.Application.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
