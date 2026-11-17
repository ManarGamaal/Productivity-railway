using TaskFlow.Application.Common.Interfaces;

namespace TaskFlow.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
