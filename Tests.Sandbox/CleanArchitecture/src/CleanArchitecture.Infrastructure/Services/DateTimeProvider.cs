using CleanArchitecture.Application.Services;

namespace CleanArchitecture.Infrastructure.Services;

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
