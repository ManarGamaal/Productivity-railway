namespace TaskFlow.Domain.Exceptions;

/// <summary>Maps to HTTP 422 (e.g. overlapping timers, stopping an already stopped timer).</summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}
