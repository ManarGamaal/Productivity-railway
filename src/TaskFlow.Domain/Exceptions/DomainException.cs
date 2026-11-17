namespace TaskFlow.Domain.Exceptions;

/// <summary>Base type for all expected, business-level failures.</summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message)
    {
    }
}
